using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace LaboratoMDM.PolicyEngine.Persistence
{
    public sealed class PolicyShortView
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    public sealed class PolicyGroupView
    {
        public string Scope { get; set; } = null!;
        public List<PolicyShortView> Policies { get; set; } = new();
    }

    public sealed class PolicyRepository(
        SqliteConnection conn,
        IEntityMapper<PolicyEntity> mapper,
        IEntityMapper<PolicyDetailsView> detailsViewMapper) : IPolicyRepository
    {
        private readonly SqliteConnection _conn = conn;
        private readonly IEntityMapper<PolicyEntity> _mapper = mapper;
        private readonly IEntityMapper<PolicyDetailsView> _detailsViewMapper = detailsViewMapper;

        public async Task<PolicyEntity?> GetById(int policyId)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM Policies WHERE Id = @id
            """);

            cmd.Parameters.AddWithValue("@id", policyId);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? _mapper.Map(reader) : null;
        }

        public async Task<PolicyEntity?> GetByIdWithDetails(int policyId)
        {
            using var cmd = _conn.CreateCommand("""
                    WITH
                -- -------------------------
                -- policy base
                -- -------------------------
                policy_base AS (
                    SELECT
                        pol.Id,
                        pol.Name,
                        pol.Scope,
                        pol.RegistryKey,
                        pol.ValueName,
                        pol.EnabledValue,
                        pol.DisabledValue,
                        pol.ParentCategoryRef,
                        pol.SupportedOnRef,
                        pol.PresentationRef,
                        pol.ClientExtension,
                        pol.Hash
                    FROM Policies pol
                    WHERE pol.Id = @id
                ),

                -- -------------------------
                -- presentation
                -- -------------------------
                presentation_base AS (
                    SELECT
                        p.Id,
                        p.PresentationId,
                        p.AdmlFile
                    FROM Presentations p
                    JOIN Policies pol ON pol.PresentationRef = p.PresentationId
                    WHERE pol.Id = @id
                ),

                -- -------------------------
                -- presentation elements
                -- -------------------------
                presentation_elements AS (
                    SELECT
                        pe.Id,
                        pe.PresentationId,
                        pe.ElementType,
                        pe.RefId,
                        pe.ParentElementId,
                        pe.DefaultValue,
                        pe.DisplayOrder,
                        pt.TextValue
                    FROM PresentationElements pe
                    LEFT JOIN PresentationTranslations pt
                        ON pt.PresentationElementId = pe.Id
                       AND pt.LangCode = 'ru-RU'
                ),

                -- -------------------------
                -- policy elements
                -- -------------------------
                policy_elements AS (
                    SELECT
                        pole.Id,
                        pole.PolicyId,
                        pole.ElementId,
                        pole.Type,
                        pole.ValueName,
                        pole.Required,
                        pole.MaxLength,
                        pole.ClientExtension,
                        pole.ValuePrefix,
                        pole.ExplicitValue,
                        pole.Additive,
                        pole.MinValue,
                        pole.MaxValue,
                        pole.StoreAsText,
                        pole.Expandable,
                        pole.MaxStrings
                    FROM PolicyElements pole
                    WHERE pole.PolicyId = :policyId
                ),

                -- -------------------------
                -- policy element items
                -- -------------------------
                policy_element_items AS (
                    SELECT
                        pei.Id,
                        pei.PolicyElementId,
                        pei.Name,
                        pei.ParentType,
                        pei.Type,
                        pei.ValueType,
                        pei.RegistryKey,
                        pei.ValueName,
                        pei.Value,
                        pei.Required,
                        pei.ParentId,
                        t.TextValue AS DisplayName
                    FROM PolicyElementItems pei
                    LEFT JOIN Translations t
                        ON t.StringId = pei.DisplayName
                       AND t.LangCode = 'ru-RU'
                )

                SELECT json_object(
                    'policy', json_object(
                        'id', pb.Id,
                        'name', pb.Name,
                        'hash', pb.Hash,
                        'scope', pb.Scope,
                        'registry', json_object(
                            'key', pb.RegistryKey,
                            'valueName', pb.ValueName,
                            'enabledValue', pb.EnabledValue,
                            'disabledValue', pb.DisabledValue
                        ),
                        'parentCategoryRef', pb.ParentCategoryRef,
                        'supportedOnRef', pb.SupportedOnRef,
                        'clientExtension', pb.ClientExtension
                    ),

                    'presentation', (
                        SELECT json_object(
                            'id', pr.Id,
                            'presentationId', pr.PresentationId,
                            'admlFile', pr.AdmlFile,
                            'elements', (
                                SELECT json_group_array(
                                    json_object(
                                        'id', pe.Id,
                                        'type', pe.ElementType,
                                        'refId', pe.RefId,
                                        'parentElementId', pe.ParentElementId,
                                        'defaultValue', pe.DefaultValue,
                                        'displayOrder', pe.DisplayOrder,
                                        'text', pe.TextValue
                                    )
                                )
                                FROM presentation_elements pe
                                WHERE pe.PresentationId = pr.Id
                            )
                        )
                        FROM presentation_base pr
                    ),

                    'policyElements', (
                        SELECT json_group_array(
                            json_object(
                                'id', pe.Id,
                                'elementId', pe.ElementId,
                                'type', pe.Type,
                                'valueName', pe.ValueName,
                                'required', pe.Required,
                                'maxLength', pe.MaxLength,
                                'items', (
                                    SELECT json_group_array(
                                        json_object(
                                            'id', i.Id,
                                            'name', i.Name,
                                            'parentType', i.ParentType,
                                            'type', i.Type,
                                            'valueType', i.ValueType,
                                            'registryKey', i.RegistryKey,
                                            'valueName', i.ValueName,
                                            'value', i.Value,
                                            'required', i.Required,
                                            'parentId', i.ParentId,
                                            'displayName', i.DisplayName
                                        )
                                    )
                                    FROM policy_element_items i
                                    WHERE i.PolicyElementId = pe.Id
                                )
                            )
                        )
                        FROM policy_elements pe
                    )
                ) as PolicyDetails
                FROM policy_base pb;
                """);

            cmd.Parameters.AddWithValue("@id", policyId);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? _mapper.Map(reader) : null;
        }

        public async Task<PolicyEntity?> GetByHash(string hash)
        {
            using var cmd = _conn.CreateCommand("""
                SELECT * FROM Policies WHERE Hash = @hash
            """);

            cmd.Parameters.AddWithValue("@hash", hash);

            using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? _mapper.Map(reader) : null;
        }

        public async Task<IReadOnlyList<PolicyEntity>> GetByHashes(IReadOnlyCollection<string> hashes)
        {
            if (hashes.Count == 0)
                return Array.Empty<PolicyEntity>();

            using var cmd = _conn.CreateCommand("""
                SELECT *
                FROM Policies
                WHERE Hash IN (
                    SELECT value FROM json_each(@hashes)
                )
            """);

            cmd.Parameters.AddWithValue("@hashes", System.Text.Json.JsonSerializer.Serialize(hashes));

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }

        public async Task<PolicyEntity> CreateIfNotExists(PolicyEntity policy)
        {
            await using var tx = await _conn.BeginTransactionAsync();

            var existing = await GetByHash(policy.Hash);
            if (existing != null) return existing;

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT INTO Policies
                (Name, DisplayName, ExplainText, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SupportedOnRef, ParentCategoryRef, PresentationRef, ClientExtension, Hash)
                VALUES
                (@name, @dn, @et, @scope, @rk, @vn, @en, @dis, @sup, @pcr, @pres, @ce, @hash);
                SELECT last_insert_rowid();
            """;

            cmd.Parameters.AddWithValue("@name", policy.Name);
            cmd.Parameters.AddWithValue("@dn", (object?)policy.DisplayName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@et", (object?)policy.ExplainText ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@scope", policy.ScopeString);
            cmd.Parameters.AddWithValue("@rk", (object?)policy.RegistryKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@vn", (object?)policy.ValueName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@en", (object?)policy.EnabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dis", (object?)policy.DisabledValue ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@sup", (object?)policy.SupportedOnRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pcr", (object?)policy.ParentCategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pres", (object?)policy.PresentationRef ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ce", (object?)policy.ClientExtension ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@hash", policy.Hash);

            policy.Id = (int)(long)(await cmd.ExecuteScalarAsync()!);

            await SavePolicyElementsAsync(policy.Id, policy.Elements, (SqliteTransaction)tx);

            await tx.CommitAsync();
            return policy;
        }

        public async Task CreatePoliciesBatch(IReadOnlyList<PolicyEntity> policies)
        {
            if (policies.Count == 0) return;

            await using var tx = await _conn.BeginTransactionAsync();

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO Policies
                (Name, DisplayName, ExplainText, Scope, RegistryKey, ValueName, EnabledValue, DisabledValue, SupportedOnRef, ParentCategoryRef, PresentationRef, ClientExtension, Hash)
                VALUES
                (@name, @dn, @et, @scope, @rk, @vn, @en, @dis, @sup, @pcr, @pres, @ce, @hash);
            """;

            var name = cmd.CreateParameter(); name.ParameterName = "@name";
            var dn = cmd.CreateParameter(); dn.ParameterName = "@dn";
            var et = cmd.CreateParameter(); et.ParameterName = "@et";
            var scope = cmd.CreateParameter(); scope.ParameterName = "@scope";
            var rk = cmd.CreateParameter(); rk.ParameterName = "@rk";
            var vn = cmd.CreateParameter(); vn.ParameterName = "@vn";
            var en = cmd.CreateParameter(); en.ParameterName = "@en";
            var dis = cmd.CreateParameter(); dis.ParameterName = "@dis";
            var sup = cmd.CreateParameter(); sup.ParameterName = "@sup";
            var pcr = cmd.CreateParameter(); pcr.ParameterName = "@pcr";
            var pres = cmd.CreateParameter(); pres.ParameterName = "@pres";
            var ce = cmd.CreateParameter(); ce.ParameterName = "@ce";
            var hash = cmd.CreateParameter(); hash.ParameterName = "@hash";


            cmd.Parameters.AddRange(new[] { name, dn, et, scope, rk, vn, en, dis, sup, pcr, pres, ce, hash });

            foreach (var p in policies)
            {
                name.Value = p.Name;
                dn.Value = (object?)p.DisplayName ?? DBNull.Value;
                et.Value = (object?)p.ExplainText ?? DBNull.Value;
                scope.Value = p.ScopeString;
                rk.Value = (object?)p.RegistryKey ?? DBNull.Value;
                vn.Value = (object?)p.ValueName ?? DBNull.Value;
                en.Value = (object?)p.EnabledValue ?? DBNull.Value;
                dis.Value = (object?)p.DisabledValue ?? DBNull.Value;
                sup.Value = (object?)p.SupportedOnRef ?? DBNull.Value;
                pcr.Value = (object?)p.ParentCategory ?? DBNull.Value;
                pres.Value = (object?)p.PresentationRef ?? DBNull.Value;
                ce.Value = (object?)p.ClientExtension ?? DBNull.Value;
                hash.Value = p.Hash;

                await cmd.ExecuteNonQueryAsync();

                // Получаем Id вставленной или существующей политики
                using var getIdCmd = _conn.CreateCommand();
                getIdCmd.Transaction = (SqliteTransaction)tx;
                getIdCmd.CommandText = "SELECT Id FROM Policies WHERE Hash = @hash";
                getIdCmd.Parameters.AddWithValue("@hash", p.Hash);
                p.Id = (int)(long)(await getIdCmd.ExecuteScalarAsync()!);

                await SavePolicyElementsAsync(p.Id, p.Elements, (SqliteTransaction)tx);
            }

            await tx.CommitAsync();
        }

        private async Task SavePolicyElementsAsync(
            int policyId,
            IEnumerable<PolicyElementEntity> elements,
            SqliteTransaction tx)
        {
            foreach (var e in elements)
            {
                using var cmd = _conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
INSERT OR IGNORE INTO PolicyElements
(PolicyId, ElementId, Type, ValueName, Required, MaxLength, ClientExtension, ValuePrefix, 
ExplicitValue, Additive, MinValue, MaxValue, StoreAsText, Expandable, MaxStrings)
VALUES
(@pid,@eid,@type,@vn,@req,@ml,@ce,@vp,@ev,@add,@min,@max,@store,@exp,@maxstr);

SELECT Id
FROM PolicyElements
WHERE PolicyId = @pid AND ElementId = @eid;
""";

                cmd.Parameters.AddWithValue("@pid", policyId);
                cmd.Parameters.AddWithValue("@eid", e.IdName);
                cmd.Parameters.AddWithValue("@type", e.Type);
                cmd.Parameters.AddWithValue("@vn", (object?)e.ValueName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@req", e.Required ? 1 : 0);
                cmd.Parameters.AddWithValue("@ml", (object?)e.MaxLength ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ce", (object?)e.ClientExtension ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@vp", (object?)e.ValuePrefix ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ev", (object?)e.ExplicitValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@add", (object?)e.Additive ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@min", (object?)e.MinValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@max", (object?)e.MaxValue ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@store", (object?)e.StoreAsText ?? DBNull.Value);

                cmd.Parameters.AddWithValue("@exp", (object?)e.Expandable ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@maxstr", (object?)e.MaxStrings ?? DBNull.Value);

                e.Id = (int)(long)(await cmd.ExecuteScalarAsync()!);

                // items
                await SavePolicyElementItemsAsync(e.Id, e.Childs, null, tx);
            }
        }

        private async Task SavePolicyElementItemsAsync(
            int elementId,
            IEnumerable<PolicyElementItemEntity> items,
            int? parentId,
            SqliteTransaction tx)
        {
            foreach (var item in items)
            {
                using var cmd = _conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO PolicyElementItems
                    (Name, PolicyElementId, ParentType, Type, ValueType,
                     RegistryKey, ValueName, Value, DisplayName, Required, ParentId)
                    VALUES
                    (@name, @eid, @pt, @type, @vt, @rk, @vn, @val, @dn, @req, @pid);
                    SELECT last_insert_rowid();
                """;

                cmd.Parameters.AddWithValue("@name", item.Name);
                cmd.Parameters.AddWithValue("@eid", elementId);
                cmd.Parameters.AddWithValue("@pt", item.ParentTypeString);
                cmd.Parameters.AddWithValue("@type", item.TypeString);
                cmd.Parameters.AddWithValue("@vt", (object?)item.ValueType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@rk", (object?)item.RegistryKey ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@vn", (object?)item.ValueName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@val", (object?)item.Value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dn", (object?)item.DisplayName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@req", (object?)item.Required ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@pid", (object?)parentId ?? DBNull.Value);

                item.Id = (int)(long)(await cmd.ExecuteScalarAsync()!);

                if (item.Childs.Count > 0)
                    await SavePolicyElementItemsAsync(elementId, item.Childs, item.Id, tx);
            }
        }

        public async Task LinkPolicyToAdmx(int policyId, int admxFileId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyAdmxMapping
                (PolicyId, AdmxFileId)
                VALUES (@p, @a)
            """;

            cmd.Parameters.AddWithValue("@p", policyId);
            cmd.Parameters.AddWithValue("@a", admxFileId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task LinkPoliciesToAdmxBatch(int admxFileId, IReadOnlyList<int> policyIds)
        {
            await using var tx = await _conn.BeginTransactionAsync();

            using var cmd = _conn.CreateCommand();
            cmd.Transaction = (SqliteTransaction)tx;
            cmd.CommandText = """
                INSERT OR IGNORE INTO PolicyAdmxMapping
                (PolicyId, AdmxFileId)
                VALUES (@p, @a)
            """;

            var p = cmd.CreateParameter(); p.ParameterName = "@p";
            var a = cmd.CreateParameter(); a.ParameterName = "@a";
            cmd.Parameters.Add(p);
            cmd.Parameters.Add(a);

            foreach (var id in policyIds)
            {
                p.Value = id;
                a.Value = admxFileId;
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<PolicyEntity>> FindApplicablePolicies(PolicyEvaluationContext context)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT *
                FROM Policies
                WHERE Scope IN ('None', 'Both', @scope)
            """;

            cmd.Parameters.AddWithValue("@scope", "Machine");

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }

        public async Task<IReadOnlyList<PolicyEntity>> GetByCategory(int categoryId)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT p.*
                FROM Policies p
                JOIN PolicyCategoryMapping pcm
                    ON pcm.PolicyId = p.Id
                WHERE pcm.CategoryId = @cid
            """;

            cmd.Parameters.AddWithValue("@cid", categoryId);

            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<PolicyEntity>();
            while (await reader.ReadAsync())
                list.Add(_mapper.Map(reader));
            return list;
        }

        public async Task<IReadOnlyList<PolicyGroupView>> GetPoliciesGroupedByScope(string langCode)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT 
                    p.Scope, 
                    json_group_array(
                        json_object(
                            'Id', p.Id,
                            'Name', p.Name,
                            'DisplayName', IFNULL(t.TextValue, p.DisplayName)
                        )
                    ) AS policies
                FROM Policies p
                LEFT JOIN Translations t
                    ON t.StringId = p.DisplayName
                   AND t.LangCode = @langCode
                GROUP BY p.Scope;
            """;

            cmd.Parameters.AddWithValue("@langCode", langCode);

            using var reader = await cmd.ExecuteReaderAsync();
            var result = new List<PolicyGroupView>();

            while (await reader.ReadAsync())
            {
                var scope = reader.GetString(0);
                var json = reader.GetString(1);

                var policies = string.IsNullOrWhiteSpace(json)
                    ? new List<PolicyShortView>()
                    : JsonSerializer.Deserialize<List<PolicyShortView>>(json)
                      ?? new List<PolicyShortView>();

                result.Add(new PolicyGroupView
                {
                    Scope = scope,
                    Policies = policies
                });
            }

            return result;
        }

        public async Task<PolicyDetailsView> GetPolicyDetailsView(long id, string langCode)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
            WITH
            policy_base AS (
                SELECT
                    pol.Id,
                    pol.Name,
                    pol.Hash,
                    pol.Scope,
                    pol.ParentCategoryRef,
                    pol.SupportedOnRef,
                    pol.ClientExtension,
                    pol.PresentationRef
                FROM Policies pol
                WHERE pol.Id = @policyId
            ),

            presentation_base AS (
                SELECT
                    p.Id,
                    p.PresentationId,
                    p.AdmlFile
                FROM Presentations p
                JOIN policy_base pb ON pb.PresentationRef = p.PresentationId
            ),

            presentation_elements AS (
                SELECT
                    pe.Id,
                    pe.PresentationId,
                    pe.ElementType AS Type,
                    pe.RefId,
                    pe.ParentElementId,
                    pe.DefaultValue,
                    pt.TextValue AS Text
                FROM PresentationElements pe
                LEFT JOIN PresentationTranslations pt
                    ON pt.PresentationElementId = pe.Id
                   AND pt.LangCode = @langCode
            ),

            policy_elements AS (
                SELECT
                    pole.Id,
                    pole.ElementId,
                    pole.Type,
                    pole.ValueName,
                    pole.Required,
                    pole.MaxLength,
                    pole.MaxStrings,
                    pole.Expandable,
                    pole.MinValue,
                    pole.MaxValue,
                    pole.ValuePrefix,
                    pole.ExplicitValue,
                    pole.Additive
                FROM PolicyElements pole
                WHERE pole.PolicyId = @policyId
            ),

            policy_element_items AS (
                SELECT
                    pei.Id,
                    pei.PolicyElementId,
                    pei.Name,
                    pei.ParentType,
                    pei.Type,
                    pei.ValueType,
                    pei.ValueName,
                    pei.Required,
                    pei.ParentId,
                    t.TextValue AS DisplayName
                FROM PolicyElementItems pei
                LEFT JOIN Translations t
                    ON t.StringId = pei.DisplayName
                   AND t.LangCode = @langCode
            )

            SELECT json_object(
                'Policy', json_object(
                    'Id', pb.Id,
                    'Name', pb.Name,
                    'Hash', pb.Hash,
                    'Scope', pb.Scope,
                    'ParentCategoryRef', pb.ParentCategoryRef,
                    'SupportedOnRef', pb.SupportedOnRef,
                    'ClientExtension', pb.ClientExtension
                ),

                'Presentation', (
                    SELECT json_object(
                        'Id', pr.Id,
                        'PresentationId', pr.PresentationId,
                        'AdmlFile', pr.AdmlFile,
                        'Elements', (
                            SELECT json_group_array(
                                json_object(
                                    'Id', pe.Id,
                                    'Type', pe.Type,
                                    'RefId', pe.RefId,
                                    'ParentElementId', pe.ParentElementId,
                                    'DefaultValue', pe.DefaultValue,
                                    'Text', pe.Text
                                )
                            )
                            FROM presentation_elements pe
                            WHERE pe.PresentationId = pr.Id
                        )
                    )
                    FROM presentation_base pr
                ),

                'PolicyElements', (
                    SELECT json_group_array(
                        json_object(
                            'Id', pe.Id,
                            'ElementId', pe.ElementId,
                            'Type', pe.Type,
                            'ValueName', pe.ValueName,
                            'Required', pe.Required,
                            'MaxLength', pe.MaxLength,
                            'MaxStrings', pe.MaxStrings,
                            'Expandable', pe.Expandable,
                            'MinValue', pe.MinValue,
                            'MaxValue', pe.MaxValue,
                            'ValuePrefix', pe.ValuePrefix,
                            'ExplicitValue', pe.ExplicitValue,
                            'Additive', pe.Additive,
                            'Items', (
                                SELECT json_group_array(
                                    json_object(
                                        'Id', i.Id,
                                        'Name', i.Name,
                                        'ParentType', i.ParentType,
                                        'Type', i.Type,
                                        'ValueType', i.ValueType,
                                        'ValueName', i.ValueName,
                                        'Required', i.Required,
                                        'ParentId', i.ParentId,
                                        'DisplayName', i.DisplayName
                                    )
                                )
                                FROM policy_element_items i
                                WHERE i.PolicyElementId = pe.Id
                            )
                        )
                    )
                    FROM policy_elements pe
                )
            ) AS PolicyDetails
            FROM policy_base pb;
            """;

            cmd.Parameters.AddWithValue("@policyId", id);
            cmd.Parameters.AddWithValue("@langCode", langCode);

            using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException($"Has no details for this policy with id: {id}");
            }

            return _detailsViewMapper.Map(reader);
        }
    }
}
