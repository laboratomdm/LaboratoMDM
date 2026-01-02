using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public sealed class PolicyMetadataRepository(
        SqliteConnection connection,
        IEntityMapper<PolicyCategoryEntity> categoryMapper,
        IEntityMapper<IReadOnlyList<PolicyCategoryView>> categoryViewMapper,
        IEntityMapper<PolicyNamespaceEntity> namespaceMapper): IPolicyMetadataRepository
    {
        private readonly SqliteConnection _connection = connection;
        private readonly IEntityMapper<PolicyCategoryEntity> _categoryMapper = categoryMapper;
        private readonly IEntityMapper<IReadOnlyList<PolicyCategoryView>> _categoryViewMapper = categoryViewMapper;
        private readonly IEntityMapper<PolicyNamespaceEntity> _namespaceMapper = namespaceMapper;

        #region Categories

        public async Task<PolicyCategoryEntity?> GetCategoryByName(
            string name)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT * FROM PolicyCategories
                WHERE Name = @name
            """;
            cmd.Parameters.AddWithValue("@name", name);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync()
                ? _categoryMapper.Map(reader)
                : null;
        }

        public async Task<PolicyCategoryEntity> CreateCategoryIfNotExists(
            PolicyCategoryEntity category)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    INSERT OR IGNORE INTO PolicyCategories
                    (Name, ParentCategoryRef)
                    VALUES (@name, @parent)
                """;

                cmd.Parameters.AddWithValue("@name", category.Name);
                cmd.Parameters.AddWithValue(
                    "@parent",
                    !string.IsNullOrWhiteSpace(category.ParentCategoryRef)
                        ? category.ParentCategoryRef
                        : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            var created = await GetCategoryByName(category.Name);
            return created
                   ?? throw new InvalidOperationException(
                       "Failed to create or load category");
        }

        public async Task CreateCategoriesBatch(IReadOnlyList<PolicyCategoryEntity> categories)
        {
            await using var tx = await _connection.BeginTransactionAsync();

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;

            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyCategories
                (Name, ParentCategoryRef, DisplayName)
                VALUES (@name, @parent, @dn)
            """;

            var name = cmd.CreateParameter();
            name.ParameterName = "@name";
            cmd.Parameters.Add(name);

            var parent = cmd.CreateParameter();
            parent.ParameterName = "@parent";
            cmd.Parameters.Add(parent);

            var dn = cmd.CreateParameter();
            dn.ParameterName = "@dn";
            cmd.Parameters.Add(dn);

            foreach (var c in categories)
            {
                name.Value = c.Name;
                parent.Value = !string.IsNullOrWhiteSpace(c.ParentCategoryRef)
                        ? c.ParentCategoryRef
                        : DBNull.Value;
                dn.Value = c.DisplayName;

                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<PolicyCategoryView>> GetCategoryTree()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
              WITH RootCategories AS (
                SELECT
                    c.Id,
                    c.Name AS CategoryName,
                    c.DisplayName,
                    c.ExplainText
                FROM PolicyCategories c
                WHERE c.ParentCategoryRef IS NULL OR c.ParentCategoryRef = ''
            ),
            -- Уровень 1: Вложенные категории
            ChildCategories AS (
                SELECT
                    c.Id,
                    c.Name AS CategoryName,
                    c.DisplayName,
                    c.ExplainText,
                    -- берём правую часть после ':' если есть
                    CASE 
                        WHEN instr(c.ParentCategoryRef, ':') > 0 THEN substr(c.ParentCategoryRef, instr(c.ParentCategoryRef, ':') + 1)
                        ELSE c.ParentCategoryRef
                    END AS ParentName
                FROM PolicyCategories c
                WHERE c.ParentCategoryRef IS NOT NULL AND c.ParentCategoryRef != ''
            )
            SELECT json_group_array(
                json_object(
                    'CategoryName', r.CategoryName,
                    'DisplayName', r.DisplayName,
                    'ExplainText', r.ExplainText,
                    'Children', (
                        SELECT json_group_array(
                            json_object(
                                'CategoryName', ch.CategoryName,
                                'DisplayName', ch.DisplayName,
                                'ExplainText', ch.ExplainText
                            )
                        )
                        FROM ChildCategories ch
                        WHERE ch.ParentName = r.CategoryName
                    )
                )
            ) AS CategoryTreeJson
            FROM RootCategories r;
            """;

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("Failed to load category tree");

            return _categoryViewMapper.Map(reader);
        }

        #endregion

        #region Namespaces

        public async Task<PolicyNamespaceEntity> CreateNamespaceIfNotExists(
            PolicyNamespaceEntity ns)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = """
                    INSERT OR IGNORE INTO PolicyNamespaces
                    (AdmxFileId, Prefix, Namespace)
                    VALUES (@admx, @prefix, @ns)
                """;

                cmd.Parameters.AddWithValue("@admx", ns.AdmxFileId);
                cmd.Parameters.AddWithValue("@prefix", ns.Prefix);
                cmd.Parameters.AddWithValue("@ns", ns.Namespace);

                await cmd.ExecuteNonQueryAsync();
            }

            using var select = _connection.CreateCommand();
            select.CommandText = """
                SELECT * FROM PolicyNamespaces
                WHERE AdmxFileId = @admx AND Prefix = @prefix
            """;

            select.Parameters.AddWithValue("@admx", ns.AdmxFileId);
            select.Parameters.AddWithValue("@prefix", ns.Prefix);

            using var reader = await select.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException(
                    "Failed to create or load namespace");

            return _namespaceMapper.Map(reader);
        }

        public async Task CreateNamespacesBatch(
            int admxFileId,
            IReadOnlyList<PolicyNamespaceEntity> namespaces)
        {
            await using var tx = await _connection.BeginTransactionAsync();

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = (SqliteTransaction) tx;

            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyNamespaces
                (AdmxFileId, Prefix, Namespace)
                VALUES (@admx, @prefix, @ns)
            """;

            var admx = cmd.CreateParameter();
            admx.ParameterName = "@admx";
            cmd.Parameters.Add(admx);

            var prefix = cmd.CreateParameter();
            prefix.ParameterName = "@prefix";
            cmd.Parameters.Add(prefix);

            var ns = cmd.CreateParameter();
            ns.ParameterName = "@ns";
            cmd.Parameters.Add(ns);

            foreach (var n in namespaces)
            {
                admx.Value = admxFileId;
                prefix.Value = n.Prefix;
                ns.Value = n.Namespace;

                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<PolicyNamespaceEntity>> GetNamespacesForAdmx(
            int admxFileId)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT * FROM PolicyNamespaces
                WHERE AdmxFileId = @id
            """;
            cmd.Parameters.AddWithValue("@id", admxFileId);

            using var reader = await cmd.ExecuteReaderAsync();
            var result = new List<PolicyNamespaceEntity>();

            while (await reader.ReadAsync())
                result.Add(_namespaceMapper.Map(reader));

            return result;
        }

        #endregion
    }
}
