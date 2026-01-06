namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IAgentPayloadBuilder
    {
        /// <summary>
        /// Генерирует базу для синхронизации по указанному пути.
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> BuildAsync(string outputPath, CancellationToken ct = default);
    }
}
