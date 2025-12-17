using LaboratoMDM.Core.Models.User;

namespace LaboratoMDM.Core.Models.Node
{
    /// <summary>
    /// Объединённая информация об узле: системная информация + пользователи + дополнительные сведения
    /// </summary>
    public class NodeFullInfo
    {
        public Guid NodeId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Системная информация
        /// </summary>
        public NodeSystemInfo SystemInfo { get; set; } = new();

        /// <summary>
        /// Пользователи узла
        /// </summary>
        public List<UserInfo> Users { get; set; } = [];

        /// <summary>
        /// Дополнительные параметры
        /// </summary>
        public DateTime LastBootTime { get; set; }  // время последней загрузки системы
        public string OSBuild { get; set; } = string.Empty; // номер билда ОС
        public bool IsDomainJoined { get; set; }  // принадлежность к домену
        public string AntivirusStatus { get; set; } = string.Empty; // состояние антивируса
        public string TimeZone { get; set; } = string.Empty; // часовой пояс
        public string Manufacturer { get; set; } = string.Empty; // производитель ПК
        public string Model { get; set; } = string.Empty; // модель ПК
        public string FirmwareVersion { get; set; } = string.Empty; // версия BIOS/UEFI

        /// <summary>
        /// Служебные флаги
        /// </summary>
        public bool IsOnline { get; set; } = true;
    }
}
