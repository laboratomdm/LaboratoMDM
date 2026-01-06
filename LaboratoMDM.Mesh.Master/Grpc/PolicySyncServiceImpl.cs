using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Laborato.Mesh;
using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Services;

namespace LaboratoMDM.Mesh.Master.Grpc;

public sealed class PolicySyncServiceImpl : PolicySyncService.PolicySyncServiceBase
{
    private const string PAYLOAD_PATH = "agent_payload.db";
    private readonly AgentPayloadBuilder _payloadBuilder;

    public PolicySyncServiceImpl(AgentPayloadBuilder payloadBuilder)
    {
        _payloadBuilder = payloadBuilder ?? throw new ArgumentNullException(nameof(payloadBuilder));
    }

    public override async Task<PolicySyncInit> StartPolicySync(PolicySyncRequest request, ServerCallContext context)
    {
        if (!File.Exists(PAYLOAD_PATH))
        {
            await _payloadBuilder.BuildAsync(PAYLOAD_PATH);
        }

        var sha256 = await SqliteIntegrityService.ComputeSha256Async(PAYLOAD_PATH);
        var size = new FileInfo(PAYLOAD_PATH).Length;
        long masterRevision = GetMasterRevision(PAYLOAD_PATH);

        return new PolicySyncInit
        {
            Mode = PolicySyncMode.SyncModeSnapshot,
            MasterRevision = masterRevision,
            Sha256 = sha256,
            PayloadSize = size
        };
    }

    public override async Task StreamPolicyPayload(Empty request, IServerStreamWriter<PolicySyncChunk> responseStream, ServerCallContext context)
    {
        if (!File.Exists(PAYLOAD_PATH))
            throw new RpcException(new Status(StatusCode.NotFound, "Payload not found"));

        const int chunkSize = 64 * 1024; // 64 KB
        var buffer = new byte[chunkSize];

        using var fs = new FileStream(PAYLOAD_PATH, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        int read;
        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken)) > 0)
        {
            bool last = fs.Position == fs.Length;
            var chunk = new PolicySyncChunk
            {
                Data = Google.Protobuf.ByteString.CopyFrom(buffer, 0, read),
                Last = last
            };
            await responseStream.WriteAsync(chunk);
        }
    }

    private static long GetMasterRevision(string payloadPath)
    {
        var cs = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = payloadPath,
            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadOnly
        }.ToString();

        using var conn = new Microsoft.Data.Sqlite.SqliteConnection(cs);
        conn.Open();

        using var cmdCheck = conn.CreateCommand();
        cmdCheck.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='PolicyRevision';";
        if (cmdCheck.ExecuteScalar() == null)
            return 0;

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(RevisionNumber) FROM PolicyRevision;";
        var result = cmd.ExecuteScalar();
        return result != null && result != DBNull.Value ? Convert.ToInt64(result) : 0;
    }
}

