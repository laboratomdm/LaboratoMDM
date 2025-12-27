using LaboratoMDM.PolicyEngine.Domain;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence;

public sealed class PresentationQueryRepository(SqliteConnection conn) : IPresentationQueryRepository
{
    private readonly SqliteConnection _conn = conn;

    #region Public Methods

    public async Task<AdmlSnapshot> GetSnapshotAsync(string admlFile)
    {
        var presentations = await LoadPresentationsAsync(admlFile);
        if (presentations.Count == 0)
            return new AdmlSnapshot { AdmlFile = admlFile, Presentations = Array.Empty<PresentationEntity>() };

        await PopulateElementsAttributesTranslationsAsync(presentations);

        return new AdmlSnapshot
        {
            AdmlFile = admlFile,
            Presentations = presentations
        };
    }

    public async Task<PresentationEntity?> GetPresentationByIdAsync(string presentationId)
    {
        var presentations = await LoadPresentationsAsync(presentationIdFilter: presentationId);
        if (presentations.Count == 0)
            return null;

        await PopulateElementsAttributesTranslationsAsync(presentations);
        return presentations[0];
    }

    public async Task<IReadOnlyList<string>> GetPresentationIdsAsync(string admlFile)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT PresentationId
            FROM Presentations
            WHERE AdmlFile = @file
        """;
        cmd.Parameters.AddWithValue("@file", admlFile);

        var list = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(reader.GetString(0));

        return list;
    }

    public async Task<bool> ExistsAsync(string presentationId, string admlFile)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT 1
            FROM Presentations
            WHERE PresentationId = @pid AND AdmlFile = @file
            LIMIT 1
        """;
        cmd.Parameters.AddWithValue("@pid", presentationId);
        cmd.Parameters.AddWithValue("@file", admlFile);

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    #endregion

    #region Private Methods

    private async Task<List<PresentationEntity>> LoadPresentationsAsync(string admlFile = "", string? presentationIdFilter = null)
    {
        var presentations = new List<PresentationEntity>();
        using var cmd = _conn.CreateCommand();

        if (!string.IsNullOrEmpty(presentationIdFilter))
        {
            cmd.CommandText = """
                SELECT Id, PresentationId, AdmlFile
                FROM Presentations
                WHERE PresentationId = @pid
            """;
            cmd.Parameters.AddWithValue("@pid", presentationIdFilter);
        }
        else
        {
            cmd.CommandText = """
                SELECT Id, PresentationId, AdmlFile
                FROM Presentations
                WHERE AdmlFile = @file
            """;
            cmd.Parameters.AddWithValue("@file", admlFile);
        }

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            presentations.Add(new PresentationEntity
            {
                Id = reader.GetInt32(0),
                PresentationId = reader.GetString(1),
                AdmlFile = reader.GetString(2)
            });
        }

        return presentations;
    }

    private async Task PopulateElementsAttributesTranslationsAsync(List<PresentationEntity> presentations)
    {
        if (presentations.Count == 0)
            return;

        // 1. Load elements
        var presentationIds = string.Join(",", presentations.Select(p => p.Id));
        var elements = new List<PresentationElementEntity>();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT Id, PresentationId, ElementType, RefId, ParentElementId, DefaultValue, DisplayOrder
                FROM PresentationElements
                WHERE PresentationId IN ({presentationIds})
                ORDER BY DisplayOrder ASC
            """;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                elements.Add(new PresentationElementEntity
                {
                    Id = reader.GetInt32(0),
                    PresentationId = reader.GetInt32(1),
                    ElementType = reader.GetString(2),
                    RefId = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ParentElementId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5),
                    DisplayOrder = reader.GetInt32(6)
                });
            }
        }

        // 2. Load attributes and translations
        var elementIds = elements.Select(e => e.Id).ToList();
        var attributes = await LoadAttributesAsync(elementIds);
        var translations = await LoadTranslationsAsync(elementIds);

        // 3. Attach attributes and translations
        foreach (var e in elements)
        {
            if (attributes.TryGetValue(e.Id, out var attrs))
                e.Attributes.AddRange(attrs);
            if (translations.TryGetValue(e.Id, out var trans))
                e.Translations.AddRange(trans);
        }

        // 4. Build parent/child graph for labels
        foreach (var p in presentations)
        {
            var elems = elements.Where(e => e.PresentationId == p.Id).ToList();
            var parents = elems.Where(e => e.ParentElementId == null).ToList();
            var labels = elems.Where(e => e.ParentElementId != null).ToList();

            foreach (var l in labels)
            {
                var parent = parents.FirstOrDefault(pEl => pEl.Id == l.ParentElementId);
                if (parent != null)
                    parent.Children.Add(l);
            }

            p.Elements.AddRange(parents);
        }
    }

    private async Task<Dictionary<int, List<PresentationElementAttributeEntity>>> LoadAttributesAsync(List<int> elementIds)
    {
        var result = new Dictionary<int, List<PresentationElementAttributeEntity>>();
        if (elementIds.Count == 0) return result;

        var ids = string.Join(",", elementIds);
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT Id, PresentationElementId, Name, Value
            FROM PresentationElementAttributes
            WHERE PresentationElementId IN ({ids})
        """;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var a = new PresentationElementAttributeEntity
            {
                Id = reader.GetInt32(0),
                PresentationElementId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Value = reader.GetString(3)
            };

            if (!result.ContainsKey(a.PresentationElementId))
                result[a.PresentationElementId] = new List<PresentationElementAttributeEntity>();
            result[a.PresentationElementId].Add(a);
        }

        return result;
    }

    private async Task<Dictionary<int, List<PresentationTranslationEntity>>> LoadTranslationsAsync(List<int> elementIds)
    {
        var result = new Dictionary<int, List<PresentationTranslationEntity>>();
        if (elementIds.Count == 0) return result;

        var ids = string.Join(",", elementIds);
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT Id, PresentationElementId, LangCode, TextValue
            FROM PresentationTranslations
            WHERE PresentationElementId IN ({ids})
        """;

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var t = new PresentationTranslationEntity
            {
                Id = reader.GetInt32(0),
                PresentationElementId = reader.GetInt32(1),
                LangCode = reader.GetString(2),
                TextValue = reader.GetString(3)
            };

            if (!result.ContainsKey(t.PresentationElementId))
                result[t.PresentationElementId] = new List<PresentationTranslationEntity>();
            result[t.PresentationElementId].Add(t);
        }

        return result;
    }

    #endregion
}
