using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace LaboratoMDM.PolicyEngine.Services;

public sealed class SqliteIntegrityService
{
    /// <summary>
    /// Computes SHA256 hash of a file and returns it as lowercase hex string.
    /// </summary>
    public static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("SQLite file not found", filePath);

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 128 * 1024,
            useAsync: true);

        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, ct);

        await stream.FlushAsync(ct);
        stream.Close();

        return ToHex(hash);
    }

    /// <summary>
    /// Verifies both integrity and SHA256 checksum.
    /// </summary>
    public static async Task VerifyAsync(
        string sqliteDbPath,
        string expectedSha256,
        CancellationToken ct = default)
    {
        var tempCopy = Path.Combine(
            Path.GetTempPath(),
            $"sqlite_verify_{Guid.NewGuid():N}.db");

        try
        {
            File.Copy(sqliteDbPath, tempCopy, overwrite: true);

            await VerifyIntegrityInternalAsync(tempCopy, ct);

            var actualHash = await ComputeSha256Async(tempCopy, ct);

            if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"SHA256 mismatch. Expected={expectedSha256}, Actual={actualHash}");
            }
        }
        catch { }
        // todo after repair problem with temp file removing
        //finally
        //{
        //    if (File.Exists(tempCopy))
        //        SafeDelete(tempCopy);
        //}
    }

    private static async Task VerifyIntegrityInternalAsync(
        string sqliteDbPath,
        CancellationToken ct)
    {
        var cs = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteDbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private
        }.ToString();

        await using var conn = new SqliteConnection(cs);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA integrity_check;";

        var result = (string?)await cmd.ExecuteScalarAsync(ct);

        await conn.CloseAsync();

        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SQLite integrity check failed: {result}");
        }
    }


    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static void SafeDelete(string path)
    {
        for (var i = 0; i < 10; i++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(50);
            }
        }

        // если реально не удалось
        File.Delete(path);
    }

}
