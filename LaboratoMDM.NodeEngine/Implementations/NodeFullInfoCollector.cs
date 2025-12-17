using LaboratoMDM.Core.Models.Node;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace LaboratoMDM.NodeEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public sealed class NodeFullInfoCollector : INodeFullInfoCollector
    {
        private readonly INodeSystemInfoCollector _systemCollector;
        private readonly IUserCollector _userCollector;
        private readonly ILogger<NodeFullInfoCollector> _logger;

        public NodeFullInfoCollector(
            INodeSystemInfoCollector systemCollector,
            IUserCollector userCollector,
            ILogger<NodeFullInfoCollector> logger)
        {
            _systemCollector = systemCollector ?? throw new ArgumentNullException(nameof(systemCollector));
            _userCollector = userCollector ?? throw new ArgumentNullException(nameof(userCollector));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();

        public NodeFullInfo Collect()
        {
            var fullInfo = new NodeFullInfo
            {
                SystemInfo = _systemCollector.Collect(),
                Users = [.. _userCollector.GetAllUsers()]
            };

            try
            {
                // Время последней загрузки
                ulong uptimeMs = GetTickCount64();
                fullInfo.LastBootTime = DateTime.UtcNow - TimeSpan.FromMilliseconds(uptimeMs);

                // Проверка домена
                fullInfo.IsDomainJoined = !string.IsNullOrEmpty(
                    System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName
                );

                // Производитель и модель
                fullInfo.Manufacturer = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemManufacturer", "Unknown"
                )?.ToString() ?? string.Empty;

                fullInfo.Model = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "SystemProductName", "Unknown"
                )?.ToString() ?? string.Empty;

                // BIOS / Firmware version
                var biosVersionObj = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion", null
                );

                if (biosVersionObj is string[] arr)
                    fullInfo.FirmwareVersion = string.Join(",", arr);
                else
                    fullInfo.FirmwareVersion = biosVersionObj?.ToString() ?? string.Empty;

                // Часовой пояс
                fullInfo.TimeZone = TimeZoneInfo.Local.StandardName;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect extended system information without WMI.");
            }

            return fullInfo;
        }
    }
}