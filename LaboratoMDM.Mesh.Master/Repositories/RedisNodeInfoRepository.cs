using LaboratoMDM.Core.Models.Node;
using StackExchange.Redis;
using System.Text.Json;

namespace LaboratoMDM.Mesh.Master.Repositories
{
    public sealed class RedisNodeInfoRepository : INodeInfoRepository
    {
        private readonly IDatabase _db;
        private const string KeyPrefix = "node:";

        public RedisNodeInfoRepository(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task UpdateNodeInfo(string agentId, NodeFullInfo info)
        {
            var json = JsonSerializer.Serialize(info);
            await _db.StringSetAsync($"{KeyPrefix}{agentId}:fullinfo", json);
        }

        public async Task<NodeFullInfo?> GetNodeInfo(string agentId)
        {
            var value = await _db.StringGetAsync($"{KeyPrefix}{agentId}:fullinfo");
            if (value.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<NodeFullInfo>(value!);
        }

        public async Task<IReadOnlyList<NodeFullInfo>> GetAllNodes()
        {
            var server = _db.Multiplexer.GetServer(
                _db.Multiplexer.GetEndPoints().First());

            var result = new List<NodeFullInfo>();
            await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*:fullinfo"))
            {
                var val = await _db.StringGetAsync(key);
                if (!val.IsNullOrEmpty)
                {
                    var info = JsonSerializer.Deserialize<NodeFullInfo>(val!);
                    if (info != null)
                        result.Add(info);
                }
            }

            return result;
        }
    }
}
