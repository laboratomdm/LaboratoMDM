using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public sealed class AdmxRepository(
        SqliteConnection conn,
        IEntityMapper<AdmxFileEntity> admxMapper,
        IEntityMapper<PolicyNamespaceEntity> nsMapper,
        IEntityMapper<PolicyCategoryEntity> categoryMapper,
        IEntityMapper<PolicyEntity> policyMapper) : IAdmxRepository
    {
        private readonly SqliteConnection _conn = conn;
        private readonly IEntityMapper<AdmxFileEntity> _admxMapper = admxMapper;
        private readonly IEntityMapper<PolicyNamespaceEntity> _nsMapper = nsMapper;
        private readonly IEntityMapper<PolicyCategoryEntity> _categoryMapper = categoryMapper;
        private readonly IEntityMapper<PolicyEntity> _policyMapper = policyMapper;

        public async Task<AdmxFileEntity?> GetByHash(string hash)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM AdmxFiles WHERE FileHash = @hash
            """);

            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? _admxMapper.Map(reader)
                : null;
        }

        public async Task<AdmxFileEntity> CreateIfNotExists(
            string fileName,
            string hash)
        {
            var existing = await GetByHash(hash);
            if (existing != null)
                return existing;

            using var cmd = _conn.CreateCommand("""
                INSERT INTO AdmxFiles (FileName, FileHash)
                VALUES (@name, @hash);
                SELECT last_insert_rowid();
            """);

            cmd.Parameters.AddWithValue("@name", fileName);
            cmd.Parameters.AddWithValue("@hash", hash);

            var id = (long)(await cmd.ExecuteScalarAsync())!;

            return new AdmxFileEntity
            {
                Id = (int)id,
                FileName = fileName,
                FileHash = hash,
                LoadedAt = DateTime.UtcNow
            };
        }

        public async Task<AdmxSnapshot> LoadSnapshot(int admxFileId)
        {
            var admx = await LoadAdmx(admxFileId);
            return admx != null
                ?
                new AdmxSnapshot
                {
                    File = admx,
                    Namespaces = await LoadNamespaces(admxFileId),
                    Categories = await LoadCategories(),
                    Policies = await LoadPolicies(admxFileId)
                } : throw new InvalidOperationException(
                    $"ADMX file {admxFileId} not found");
        }

        private async Task<AdmxFileEntity?> LoadAdmx(int id)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM AdmxFiles WHERE Id = @id
            """);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? _admxMapper.Map(reader)
                : null;
        }

        private async Task<IReadOnlyList<PolicyNamespaceEntity>> LoadNamespaces(
            int admxFileId)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM PolicyNamespaces
                WHERE AdmxFileId = @id
            """);

            cmd.Parameters.AddWithValue("@id", admxFileId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyNamespaceEntity>();

            while (await reader.ReadAsync())
                list.Add(_nsMapper.Map(reader));

            return list;
        }

        private async Task<IReadOnlyList<PolicyCategoryEntity>> LoadCategories()
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM PolicyCategories
            """);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyCategoryEntity>();

            while (await reader.ReadAsync())
                list.Add(_categoryMapper.Map(reader));

            return list;
        }

        private async Task<IReadOnlyList<PolicyEntity>> LoadPolicies(
            int admxFileId)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT p.*
                FROM Policies p
                JOIN PolicyAdmxMapping m ON m.PolicyId = p.Id
                WHERE m.AdmxFileId = @id
            """);

            cmd.Parameters.AddWithValue("@id", admxFileId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();

            while (await reader.ReadAsync())
                list.Add(_policyMapper.Map(reader));

            return list;
        }

        // TODO: soft delete позже
        public async Task Delete(int admxFileId)
        {
            await using var tx = await _conn.BeginTransactionAsync();

            try
            {
                using var cmd = _conn.CreateCommand("""
                    DELETE FROM AdmxFiles WHERE Id = @id
                """);

                cmd.Parameters.AddWithValue("@id", admxFileId);
                await cmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<IReadOnlyList<AdmxSnapshot>> LoadAllSnapshots()
        {
            // Получаем все ADMX-файлы
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM AdmxFiles
            """);

            var snapshots = new List<AdmxSnapshot>();

            using var reader = await cmd.ExecuteReaderAsync();
            var admxFiles = new List<AdmxFileEntity>();

            while (await reader.ReadAsync())
            {
                admxFiles.Add(_admxMapper.Map(reader));
            }

            // Для каждого ADMX файла создаём полный snapshot
            foreach (var file in admxFiles)
            {
                var snapshot = await LoadSnapshot(file.Id);
                snapshots.Add(snapshot);
            }

            return snapshots;
        }
    }
}
