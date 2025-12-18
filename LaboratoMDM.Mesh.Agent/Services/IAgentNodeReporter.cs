namespace LaboratoMDM.Agent.Services
{
    public interface IAgentNodeReporter
    {
        Task SendOnceAsync(CancellationToken ct = default);
        Task StartStreamingAsync(CancellationToken ct = default);
    }
}

