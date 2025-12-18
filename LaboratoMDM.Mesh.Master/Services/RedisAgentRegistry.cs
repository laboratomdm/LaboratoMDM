using LaboratoMDM.Mesh.Master.Models;
using StackExchange.Redis;
using System.Text.Json;

namespace LaboratoMDM.Mesh.Master.Services
{
    public sealed class RedisAgentRegistry : IAgentRegistry
    {
        private readonly IDatabase _db;
        private readonly IServer _server;

        private const string Prefix = "agent:";
        private static readonly TimeSpan HeartbeatTtl = TimeSpan.FromSeconds(30);

        public RedisAgentRegistry(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
            _server = redis.GetServer(redis.GetEndPoints().First());
        }

        private static string InfoKey(string agentId) => $"{Prefix}{agentId}:info";
        private static string HeartbeatKey(string agentId) => $"{Prefix}{agentId}:heartbeat";

        public async Task RegisterAgentAsync(AgentInfo agent)
        {
            var json = JsonSerializer.Serialize(agent);

            var batch = _db.CreateBatch();

            var tasks = new List<Task>
            {
                batch.StringSetAsync(InfoKey(agent.AgentId), json),
                batch.StringSetAsync(HeartbeatKey(agent.AgentId), DateTime.UtcNow.ToString("O"), HeartbeatTtl)
            };

            batch.Execute();

            await Task.WhenAll(tasks);
        }

        public async Task UpdateHeartbeatAsync(string agentId)
        {
            await _db.StringSetAsync(
                HeartbeatKey(agentId),
                DateTime.UtcNow.ToString("O"),
                HeartbeatTtl);
        }

        public async Task<bool> IsAgentAliveAsync(string agentId)
        {
            return await _db.KeyExistsAsync(HeartbeatKey(agentId));
        }


        public async Task<AgentInfo?> GetAgentAsync(string agentId)
        {
            var value = await _db.StringGetAsync(InfoKey(agentId));

            return value.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<AgentInfo>(value!);
        }

        public async Task<IReadOnlyList<AgentInfo>> GetAllAgentsAsync()
        {
            var result = new List<AgentInfo>();

            await foreach (var key in _server.KeysAsync(
                pattern: $"{Prefix}*:info"))
            {
                var value = await _db.StringGetAsync(key);

                if (!value.IsNullOrEmpty)
                {
                    var agent = JsonSerializer.Deserialize<AgentInfo>(value!);
                    if (agent != null)
                        result.Add(agent);
                }
            }

            return result;
        }
    }
}
