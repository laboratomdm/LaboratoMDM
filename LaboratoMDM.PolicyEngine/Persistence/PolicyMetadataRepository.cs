using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public sealed class PolicyMetadataRepository(
        SqliteConnection connection,
        IEntityMapper<PolicyCategoryEntity> categoryMapper,
        IEntityMapper<PolicyNamespaceEntity> namespaceMapper): IPolicyMetadataRepository
    {
        private readonly SqliteConnection _connection = connection;
        private readonly IEntityMapper<PolicyCategoryEntity> _categoryMapper = categoryMapper;
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
                    (Name, ParentCategoryId)
                    VALUES (@name, @parent)
                """;

                cmd.Parameters.AddWithValue("@name", category.Name);
                cmd.Parameters.AddWithValue(
                    "@parent",
                    category.ParentCategoryId.HasValue
                        ? category.ParentCategoryId.Value
                        : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }

            var created = await GetCategoryByName(category.Name);
            return created
                   ?? throw new InvalidOperationException(
                       "Failed to create or load category");
        }

        public async Task<IReadOnlyList<PolicyCategoryEntity>> GetCategoryTree()
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = """
                SELECT * FROM PolicyCategories
                ORDER BY ParentCategoryId, Name
            """;

            using var reader = await cmd.ExecuteReaderAsync();
            var result = new List<PolicyCategoryEntity>();

            while (await reader.ReadAsync())
                result.Add(_categoryMapper.Map(reader));

            return result;
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
