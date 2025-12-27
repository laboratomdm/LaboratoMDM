using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence;

public sealed class AdmlSnapshotWriter : IAdmlSnapshotWriter
{
    private readonly SqliteConnection _conn;

    public AdmlSnapshotWriter(SqliteConnection conn)
    {
        _conn = conn;
    }

    public async Task SaveSnapshot(AdmlSnapshot snapshot)
    {
        await using var tx = await _conn.BeginTransactionAsync();

        try
        {
            var presentationIdMap = new Dictionary<PresentationEntity, int>();
            var elementIdMap = new Dictionary<PresentationElementEntity, int>();

            // --- Presentations ---
            foreach (var p in snapshot.Presentations)
            {
                p.Id = await GetOrInsertPresentationAsync(p, snapshot.AdmlFile, (SqliteTransaction)tx);
                presentationIdMap[p] = p.Id;
            }

            // --- Elements ---
            foreach (var p in snapshot.Presentations)
            {
                foreach (var e in p.Elements)
                {
                    e.Id = await GetOrInsertElementAsync(e, p.Id, null, (SqliteTransaction)tx);
                    elementIdMap[e] = e.Id;

                    // вставка дочерних элементов (label, defaultValue)
                    foreach (var child in e.Children)
                    {
                        child.Id = await GetOrInsertElementAsync(child, p.Id, e.Id, (SqliteTransaction)tx);
                        elementIdMap[child] = child.Id;
                    }
                }
            }

            // --- Attributes ---
            foreach (var e in elementIdMap.Keys)
            {
                foreach (var a in e.Attributes)
                {
                    await InsertAttributeAsync(a, e.Id, (SqliteTransaction)tx);
                }
            }

            // --- Translations ---
            foreach (var e in elementIdMap.Keys)
            {
                foreach (var t in e.Translations)
                {
                    await InsertTranslationAsync(t, e.Id, (SqliteTransaction)tx);
                }
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<int> GetOrInsertPresentationAsync(PresentationEntity p, string admlFile, SqliteTransaction tx)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;

        cmd.CommandText = @"
INSERT INTO Presentations (PresentationId, AdmlFile)
VALUES (@pid, @file)
ON CONFLICT(PresentationId, AdmlFile) DO NOTHING;
SELECT Id FROM Presentations WHERE PresentationId = @pid AND AdmlFile = @file;
";
        cmd.Parameters.AddWithValue("@pid", p.PresentationId);
        cmd.Parameters.AddWithValue("@file", admlFile);

        var id = (long)await cmd.ExecuteScalarAsync()!;
        return (int)id;
    }

    private async Task<int> GetOrInsertElementAsync(PresentationElementEntity e, int presId, int? parentId, SqliteTransaction tx)
    {
        // Проверяем существование по PresentationId + ElementType + RefId + DisplayOrder + ParentElementId
        await using var selCmd = _conn.CreateCommand();
        selCmd.Transaction = tx;
        selCmd.CommandText = @"
SELECT Id FROM PresentationElements
WHERE PresentationId = @pres
  AND ElementType = @type
  AND IFNULL(RefId,'') = IFNULL(@ref,'')
  AND DisplayOrder = @ord
  AND IFNULL(ParentElementId,0) = IFNULL(@parent,0);
";
        selCmd.Parameters.AddWithValue("@pres", presId);
        selCmd.Parameters.AddWithValue("@type", e.ElementType);
        selCmd.Parameters.AddWithValue("@ref", (object?)e.RefId ?? DBNull.Value);
        selCmd.Parameters.AddWithValue("@ord", e.DisplayOrder);
        selCmd.Parameters.AddWithValue("@parent", (object?)parentId ?? DBNull.Value);

        var existing = await selCmd.ExecuteScalarAsync();
        if (existing != null)
            return Convert.ToInt32(existing);

        // Вставляем
        await using var insCmd = _conn.CreateCommand();
        insCmd.Transaction = tx;
        insCmd.CommandText = @"
INSERT INTO PresentationElements
(PresentationId, ElementType, RefId, DisplayOrder, ParentElementId, DefaultValue)
VALUES (@pres, @type, @ref, @ord, @parent, @def);
SELECT last_insert_rowid();
";
        insCmd.Parameters.AddWithValue("@pres", presId);
        insCmd.Parameters.AddWithValue("@type", e.ElementType);
        insCmd.Parameters.AddWithValue("@ref", (object?)e.RefId ?? DBNull.Value);
        insCmd.Parameters.AddWithValue("@ord", e.DisplayOrder);
        insCmd.Parameters.AddWithValue("@parent", (object?)parentId ?? DBNull.Value);
        insCmd.Parameters.AddWithValue("@def", (object?)e.DefaultValue ?? DBNull.Value);

        var id = (long)await insCmd.ExecuteScalarAsync()!;
        return (int)id;
    }

    private async Task InsertAttributeAsync(PresentationElementAttributeEntity attr, int elementId, SqliteTransaction tx)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;

        cmd.CommandText = @"
INSERT INTO PresentationElementAttributes (PresentationElementId, Name, Value)
VALUES (@eid, @name, @val)
ON CONFLICT(PresentationElementId, Name) DO NOTHING;
";
        cmd.Parameters.AddWithValue("@eid", elementId);
        cmd.Parameters.AddWithValue("@name", attr.Name);
        cmd.Parameters.AddWithValue("@val", attr.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task InsertTranslationAsync(PresentationTranslationEntity t, int elementId, SqliteTransaction tx)
    {
        await using var cmd = _conn.CreateCommand();
        cmd.Transaction = tx;

        cmd.CommandText = @"
INSERT INTO PresentationTranslations (PresentationElementId, LangCode, TextValue)
VALUES (@eid, @lang, @text)
ON CONFLICT(PresentationElementId, LangCode) DO NOTHING;
";
        cmd.Parameters.AddWithValue("@eid", elementId);
        cmd.Parameters.AddWithValue("@lang", t.LangCode);
        cmd.Parameters.AddWithValue("@text", t.TextValue);

        await cmd.ExecuteNonQueryAsync();
    }
}
