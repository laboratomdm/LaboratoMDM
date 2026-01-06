using Microsoft.Data.Sqlite;

namespace LaboratoMDM.PolicyEngine.Persistence;

public sealed class AgentPayloadBuilder
{
    private readonly string _masterDbPath;

    public AgentPayloadBuilder(string masterDbPath)
    {
        _masterDbPath = masterDbPath
            ?? throw new ArgumentNullException(nameof(masterDbPath));
    }

    public async Task<string> BuildAsync(
        string outputPath,
        CancellationToken ct = default)
    {
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var payloadConnStr = new SqliteConnectionStringBuilder
        {
            DataSource = outputPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private
        }.ToString();

        await using var conn = new SqliteConnection(payloadConnStr);
        await conn.OpenAsync(ct);

        await ExecutePragmasAsync(conn, ct);
        await CreateSchemaAsync(conn, ct);
        await AttachMasterAsync(conn, ct);
        await CopyPoliciesAsync(conn, ct);
        await CopyPolicyElementsAsync(conn, ct);
        await CopyPolicyElementItemsAsync(conn, ct);
        await FixupPolicyElementItemParentsAsync(conn, ct);
        await WriteRevisionAsync(conn, ct);
        await FinalizePayloadAsync(conn, ct);

        return outputPath;
    }

    // PRAGMA
    private static async Task ExecutePragmasAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        PRAGMA journal_mode = OFF;
        PRAGMA synchronous = OFF;
        PRAGMA temp_store = MEMORY;
        PRAGMA locking_mode = EXCLUSIVE;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // SCHEMA
    private static async Task CreateSchemaAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        CREATE TABLE Policies (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            DisplayName TEXT,
            ExplainText TEXT,
            Scope TEXT NOT NULL,
            RegistryKey TEXT NOT NULL,
            ValueName TEXT NOT NULL,
            EnabledValue INTEGER,
            DisabledValue INTEGER,
            SupportedOnRef TEXT,
            ParentCategoryRef TEXT,
            PresentationRef TEXT,
            ClientExtension TEXT,
            Hash TEXT NOT NULL,
            UNIQUE(Name, Hash)
        );

        CREATE TABLE PolicyElements (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            PolicyId INTEGER NOT NULL,
            ElementId TEXT NOT NULL,
            RegistryKey TEXT,
            Type TEXT NOT NULL,
            ValueName TEXT,
            MaxLength INTEGER,
            Required INTEGER,
            ClientExtension TEXT,
            ValuePrefix TEXT,
            ExplicitValue BOOLEAN,
            Additive BOOLEAN,
            MinValue BIGINT,
            MaxValue BIGINT,
            StoreAsText BOOLEAN,
            Expandable BOOLEAN,
            MaxStrings INTEGER,
            UNIQUE (PolicyId, ElementId)
        );

        CREATE TABLE PolicyElementItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            PolicyElementId INTEGER NOT NULL,
            ParentType TEXT NOT NULL,
            Type TEXT NOT NULL,
            ValueType TEXT,
            RegistryKey TEXT,
            ValueName TEXT,
            Value TEXT,
            DisplayName TEXT,
            Required BOOLEAN,
            ParentId INTEGER
        );

        CREATE TABLE Meta (
            Key TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // ATTACH MASTER
    private async Task AttachMasterAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = "ATTACH DATABASE @path AS master;";
        cmd.Parameters.AddWithValue("@path", _masterDbPath);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    // COPY POLICIES
    private static async Task CopyPoliciesAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        INSERT INTO Policies (
            Name, DisplayName, ExplainText, Scope,
            RegistryKey, ValueName,
            EnabledValue, DisabledValue,
            SupportedOnRef, ParentCategoryRef,
            PresentationRef, ClientExtension, Hash
        )
        SELECT
            Name, DisplayName, ExplainText, Scope,
            RegistryKey, ValueName,
            EnabledValue, DisabledValue,
            SupportedOnRef, ParentCategoryRef,
            PresentationRef, ClientExtension, Hash
        FROM master.Policies;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // COPY ELEMENTS
    private static async Task CopyPolicyElementsAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        INSERT INTO PolicyElements (
            PolicyId,
            ElementId, Type, RegistryKey, ValueName,
            MaxLength, Required, ClientExtension,
            ValuePrefix, ExplicitValue, Additive,
            MinValue, MaxValue, StoreAsText,
            Expandable, MaxStrings
        )
        SELECT
            p.Id,
            pe.ElementId, pe.Type, pe.RegistryKey, pe.ValueName,
            pe.MaxLength, pe.Required, pe.ClientExtension,
            pe.ValuePrefix, pe.ExplicitValue, pe.Additive,
            pe.MinValue, pe.MaxValue, pe.StoreAsText,
            pe.Expandable, pe.MaxStrings
        FROM master.PolicyElements pe
        JOIN master.Policies mp ON mp.Id = pe.PolicyId
        JOIN Policies p ON p.Hash = mp.Hash;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // COPY ITEMS (без ParentId)
    private static async Task CopyPolicyElementItemsAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        INSERT INTO PolicyElementItems (
            Name, PolicyElementId,
            ParentType, Type, ValueType,
            RegistryKey, ValueName, Value,
            DisplayName, Required
        )
        SELECT
            pei.Name,
            ape.Id,
            pei.ParentType, pei.Type, pei.ValueType,
            pei.RegistryKey, pei.ValueName, pei.Value,
            pei.DisplayName, pei.Required
        FROM master.PolicyElementItems pei
        JOIN master.PolicyElements mpe ON mpe.Id = pei.PolicyElementId
        JOIN master.Policies mp ON mp.Id = mpe.PolicyId
        JOIN Policies p ON p.Hash = mp.Hash
        JOIN PolicyElements ape
          ON ape.PolicyId = p.Id
         AND ape.ElementId = mpe.ElementId;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // REVISION
    private static async Task WriteRevisionAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        INSERT INTO Meta(Key, Value)
        SELECT 'revision', RevisionNumber
        FROM master.PolicyRevision
        ORDER BY RevisionNumber DESC
        LIMIT 1;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // ParentId FIXup
    private static async Task FixupPolicyElementItemParentsAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        UPDATE PolicyElementItems AS child
        SET ParentId = (
            SELECT parent_payload.Id
            FROM master.PolicyElementItems parent_master
            JOIN master.PolicyElementItems child_master
                ON child_master.ParentId = parent_master.Id

            JOIN master.PolicyElements mpe
                ON mpe.Id = child_master.PolicyElementId

            JOIN master.Policies mp
                ON mp.Id = mpe.PolicyId

            JOIN Policies p
                ON p.Hash = mp.Hash

            JOIN PolicyElements pe_payload
                ON pe_payload.PolicyId = p.Id
               AND pe_payload.ElementId = mpe.ElementId

            JOIN PolicyElementItems parent_payload
                ON parent_payload.PolicyElementId = pe_payload.Id
               AND parent_payload.Name = parent_master.Name
               AND parent_payload.ParentType = parent_master.ParentType

            WHERE
                child.PolicyElementId = pe_payload.Id
                AND child.Name = child_master.Name
                AND child.ParentType = child_master.ParentType
        )
        WHERE ParentId IS NULL
          AND ParentType != 'elements';
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }

    // FINALIZE
    private static async Task FinalizePayloadAsync(
        SqliteConnection conn,
        CancellationToken ct)
    {
        var sql = """
        DETACH DATABASE master;
        VACUUM;
        """;

        await new SqliteCommand(sql, conn).ExecuteNonQueryAsync(ct);
    }
}
