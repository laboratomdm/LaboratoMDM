using LaboratoMDM.PolicyEngine.Domain;

namespace LaboratoMDM.PolicyEngine.Persistence.Abstractions
{
    public interface IAdmxRepository
    {
        /// <summary>
        /// Возвращает ADMX файл по хэшу, либо null
        /// </summary>
        Task<AdmxFileEntity?> GetByHash(string fileHash);

        /// <summary>
        /// Создаёт запись ADMX файла (если не существует)
        /// </summary>
        Task<AdmxFileEntity> CreateIfNotExists(
            string fileName,
            string fileHash);

        /// <summary>
        /// Возвращает все политики, категории и namespaces,
        /// загруженные из данного ADMX файла
        /// </summary>
        Task<AdmxSnapshot> LoadSnapshot(int admxFileId);

        /// <summary>
        /// Удаляет ADMX файл и всё связанное (CASCADE)
        /// </summary>
        Task Delete(int admxFileId);
    }

}
