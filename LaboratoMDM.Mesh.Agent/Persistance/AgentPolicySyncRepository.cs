#nullable enable

using LaboratoMDM.PolicyEngine.Services;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.Mesh.Agent.Persistance
{
    /// <summary>
    /// Репозиторий для синхронизации базы политик агента с готовым payload SQLite.
    /// Все старые tmp и бэкапы сохраняются в папку backup.
    /// </summary>
    public sealed class AgentPolicySyncRepository
    {
        private readonly string _agentDbPath;
        private readonly string _backupFolder;

        public AgentPolicySyncRepository(string agentDbPath)
        {
            _agentDbPath = agentDbPath ?? throw new ArgumentNullException(nameof(agentDbPath));

            // backup folder рядом с базой
            _backupFolder = Path.Combine(Path.GetDirectoryName(agentDbPath) ?? ".", "backup");
            Directory.CreateDirectory(_backupFolder);
        }

        /// <summary>
        /// Применяет payload SQLite в локальную базу агента.
        /// Выполняет проверку SHA256 + integrity и атомарную замену базы.
        /// Сохраняет старые версии в backup.
        /// </summary>
        public async Task ApplyPayloadAsync(
            string payloadPath,
            string expectedSha256,
            long revision,
            CancellationToken ct = default)
        {
            if (!File.Exists(payloadPath))
                throw new FileNotFoundException("Payload file not found", payloadPath);

            // Проверяем целостность ОРИГИНАЛА
            await SqliteIntegrityService.VerifyAsync(payloadPath, expectedSha256, ct);

            // Делаем КОПИЮ payload (ТОЛЬКО её будем ATTACH)
            var payloadCopy = Path.Combine(
                _backupFolder,
                $"payload_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.db");

            File.Copy(payloadPath, payloadCopy, overwrite: true);

            try
            {
                // Готовим временную рабочую базу
                var tmpDb = Path.Combine(
                    _backupFolder,
                    $"tmp_{DateTime.UtcNow:yyyyMMdd_HHmmssfff}.db");

                if (File.Exists(_agentDbPath))
                    File.Copy(_agentDbPath, tmpDb, overwrite: true);
                else
                    File.Copy(payloadCopy, tmpDb, overwrite: true);

                // MERGE ТОЛЬКО из копии payload
                MergePayload(tmpDb, payloadCopy);

                // Атомарная замена
                SaveBackupAndReplace(tmpDb, _agentDbPath);

                // Обновление ревизии
                UpdateLocalRevision(revision);
            }
            finally
            {
                //Безопасно удаляем payload-копию
                //SafeDelete(payloadCopy);
            }
        }

        private static void MergePayload(string targetDbPath, string payloadPath)
        {
            var targetCs = new SqliteConnectionStringBuilder
            {
                DataSource = targetDbPath,
                Mode = SqliteOpenMode.ReadWrite,
                Cache = SqliteCacheMode.Private
            }.ToString();

            var payloadCs = new SqliteConnectionStringBuilder
            {
                DataSource = payloadPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Private
            }.ToString();

            using var conn = new SqliteConnection(targetCs);
            conn.Open();

            using var tx = conn.BeginTransaction();

            // ATTACH через URI с mode=ReadOnly
            using (var attach = conn.CreateCommand())
            {
                // payload path должен быть абсолютным
                var absPath = Path.GetFullPath(payloadPath).Replace("'", "''");
                attach.CommandText = $"ATTACH DATABASE '{absPath}' AS payload;";
                attach.ExecuteNonQuery();
            }

            void Exec(string sql)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }

            // Предполагаем, что payload тоже READONLY, поэтому можно SELECT без конфликтов
            Exec("DELETE FROM Policies;");
            Exec("INSERT INTO Policies SELECT * FROM payload.Policies;");

            Exec("DELETE FROM PolicyElements;");
            Exec("INSERT INTO PolicyElements SELECT * FROM payload.PolicyElements;");

            Exec("DELETE FROM PolicyElementItems;");
            Exec("INSERT INTO PolicyElementItems SELECT * FROM payload.PolicyElementItems;");

            //Exec("DETACH DATABASE payload;");

            tx.Commit();
        }


        private void SaveBackupAndReplace(string tmpDb, string targetDb)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
            var backupPath = Path.Combine(_backupFolder, $"backup_{timestamp}.db");

            if (File.Exists(targetDb))
                File.Copy(targetDb, backupPath, overwrite: false); // сохраняем старую версию

            File.Copy(tmpDb, targetDb, overwrite: true); // заменяем основную базу
        }

        private void UpdateLocalRevision(long masterRevision)
        {
            if (!File.Exists(_agentDbPath))
                return;

            var cs = new SqliteConnectionStringBuilder { DataSource = _agentDbPath }.ToString();

            using var conn = new SqliteConnection(cs);
            conn.Open();

            using var cmdCheck = conn.CreateCommand();
            cmdCheck.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Versions';";
            var exists = cmdCheck.ExecuteScalar() != null;

            if (!exists) return;

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO Versions(SourceRevision, InstalledAt)
                VALUES (@rev, CURRENT_DATE);";
            cmd.Parameters.AddWithValue("@rev", masterRevision);

            cmd.ExecuteNonQuery();
        }
    }
}