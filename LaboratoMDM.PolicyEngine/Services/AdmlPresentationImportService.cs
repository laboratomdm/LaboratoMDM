using LaboratoMDM.PolicyEngine.Persistence;
using LaboratoMDM.PolicyEngine.Persistence.Abstractions;
using LaboratoMDM.PolicyEngine.Services.Abstractions;

namespace LaboratoMDM.PolicyEngine.Services
{
    /// <summary>
    /// Сервис для загрузки ADML и сохранения presentations в БД
    /// </summary>
    public sealed class AdmlPresentationImportService : IAdmlPresentationImportService
    {
        private readonly IAdmlSnapshotWriter _writer;

        public AdmlPresentationImportService(IAdmlSnapshotWriter writer)
        {
            _writer = writer;
        }

        /// <summary>
        /// Загружает ADML файл, собирает snapshot и сохраняет его в базу
        /// </summary>
        public async Task LoadAndSaveAsync(string filePath)
        {
            AdmlPresentationProvider provider = new AdmlPresentationProvider(filePath);
            var snapshot = provider.Parse();
            await _writer.SaveSnapshot(snapshot);
        }
    }
}
