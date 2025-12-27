using LaboratoMDM.Core.Models.Policy;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Persistence.Mapping;
using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence;

public sealed class TranslationRepository(
    SqliteConnection conn,
    IEntityMapper<Translation> mapper)
    : ITranslationRepository
{
    private readonly SqliteConnection _conn = conn;
    private readonly IEntityMapper<Translation> _mapper = mapper;
    private bool _disposed;

    public async Task<Translation?> GetByIdAsync(string stringId, string langCode)
    {
        using var cmd = _conn.CreateCommand("""
            SELECT *
            FROM Translations
            WHERE StringId = @stringId
              AND LangCode = @langCode
        """);

        cmd.Parameters.AddWithValue("@stringId", stringId);
        cmd.Parameters.AddWithValue("@langCode", langCode);

        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync()
            ? _mapper.Map(reader)
            : null;
    }

    public async Task<IReadOnlyList<Translation>> GetByLanguageAsync(string langCode)
    {
        using var cmd = _conn.CreateCommand("""
            SELECT *
            FROM Translations
            WHERE LangCode = @langCode
            ORDER BY StringId
        """);

        cmd.Parameters.AddWithValue("@langCode", langCode);

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Translation>();

        while (await reader.ReadAsync())
            list.Add(_mapper.Map(reader));

        return list;
    }

    public async Task<IReadOnlyList<Translation>> GetByAdmlFileAsync(string admlFilename)
    {
        using var cmd = _conn.CreateCommand("""
            SELECT *
            FROM Translations
            WHERE AdmlFilename = @file
            ORDER BY LangCode, StringId
        """);

        cmd.Parameters.AddWithValue("@file", admlFilename);

        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Translation>();

        while (await reader.ReadAsync())
            list.Add(_mapper.Map(reader));

        return list;
    }

    public async Task InsertAsync(Translation translation)
    {
        using var cmd = _conn.CreateCommand("""
            INSERT OR REPLACE INTO Translations
            (StringId, LangCode, TextValue, AdmlFilename, CreatedAt)
            VALUES
            (@stringId, @langCode, @textValue, @admlFile, @createdAt)
        """);

        cmd.Parameters.AddWithValue("@stringId", translation.StringId);
        cmd.Parameters.AddWithValue("@langCode", translation.LangCode);
        cmd.Parameters.AddWithValue("@textValue", translation.TextValue);
        cmd.Parameters.AddWithValue("@admlFile",
            translation.AdmlFilename ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@createdAt",
            translation.CreatedAt ?? DateTime.UtcNow);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task InsertBatchAsync(IEnumerable<Translation> translations)
    {
        await using var tx = await _conn.BeginTransactionAsync();

        try
        {
            using var cmd = _conn.CreateCommand("""
                INSERT OR REPLACE INTO Translations
                (StringId, LangCode, TextValue, AdmlFilename, CreatedAt)
                VALUES
                (@stringId, @langCode, @textValue, @admlFile, @createdAt)
            """);

            var pStringId = cmd.Parameters.Add("@stringId", SqliteType.Text);
            var pLangCode = cmd.Parameters.Add("@langCode", SqliteType.Text);
            var pTextValue = cmd.Parameters.Add("@textValue", SqliteType.Text);
            var pAdmlFile = cmd.Parameters.Add("@admlFile", SqliteType.Text);
            var pCreatedAt = cmd.Parameters.Add("@createdAt", SqliteType.Text);

            foreach (var t in translations)
            {
                pStringId.Value = t.StringId;
                pLangCode.Value = t.LangCode;
                pTextValue.Value = t.TextValue;
                pAdmlFile.Value = t.AdmlFilename ?? (object)DBNull.Value;
                pCreatedAt.Value = t.CreatedAt ?? DateTime.UtcNow;

                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string stringId, string langCode)
    {
        using var cmd = _conn.CreateCommand("""
            SELECT 1
            FROM Translations
            WHERE StringId = @stringId
              AND LangCode = @langCode
            LIMIT 1
        """);

        cmd.Parameters.AddWithValue("@stringId", stringId);
        cmd.Parameters.AddWithValue("@langCode", langCode);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<int> DeleteByStringIdAsync(string stringId)
    {
        using var cmd = _conn.CreateCommand("""
            DELETE FROM Translations
            WHERE StringId = @stringId
        """);

        cmd.Parameters.AddWithValue("@stringId", stringId);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteByLanguageAsync(string langCode)
    {
        using var cmd = _conn.CreateCommand("""
            DELETE FROM Translations
            WHERE LangCode = @langCode
        """);

        cmd.Parameters.AddWithValue("@langCode", langCode);
        return await cmd.ExecuteNonQueryAsync();
    }

    public async Task<int> CountByLanguageAsync(string langCode)
    {
        using var cmd = _conn.CreateCommand("""
            SELECT COUNT(*)
            FROM Translations
            WHERE LangCode = @langCode
        """);

        cmd.Parameters.AddWithValue("@langCode", langCode);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _conn.Dispose();
        _disposed = true;
    }
}
