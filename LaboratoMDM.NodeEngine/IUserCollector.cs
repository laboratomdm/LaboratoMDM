using LaboratoMDM.Core.Models.User;

namespace LaboratoMDM.NodeEngine
{
    public interface IUserCollector
    {
        /// <summary>
        /// Возвращает список всех локальных пользователей на машине.
        /// </summary>
        IReadOnlyList<UserInfo> GetAllUsers();
    }
}
