using LaboratoMDM.Core.Models;
using LaboratoMDM.NodeEngine;
using Microsoft.Extensions.Logging;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace LaboratoMDM.NodeEngine.Implementations
{
    [SupportedOSPlatform("windows")]
    public class NodeSystemInfoCollector(ILogger<NodeSystemInfoCollector> logger) : INodeSystemInfoCollector
    {
        private readonly ILogger<NodeSystemInfoCollector> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public NodeSystemInfo Collect()
        {
            var info = new NodeSystemInfo
            {
                HostName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString()
            };

            _logger.LogInformation("Collecting hardware info for {Host}", info.HostName);

            CollectHardwareInfo(info);
            CollectMotherboardInfo(info);
            CollectNetworkInfo(info);

            return info;
        }

        private void CollectHardwareInfo(NodeSystemInfo info)
        {
            try
            {
                // CPU
                using var cpuSearcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores FROM Win32_Processor");
                foreach (var item in cpuSearcher.Get())
                {
                    info.CPU = item["Name"]?.ToString() ?? string.Empty;
                }

                // RAM
                using var ramSearcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory");
                long totalRam = 0;
                foreach (var item in ramSearcher.Get())
                {
                    totalRam += Convert.ToInt64(item["Capacity"]);
                }
                info.RAMGb = (int)(totalRam / (1024 * 1024 * 1024));

                // Disks
                using var diskSearcher = new ManagementObjectSearcher("SELECT Model, Size, SerialNumber FROM Win32_DiskDrive");
                foreach (var item in diskSearcher.Get())
                {
                    string model = item["Model"]?.ToString() ?? "Unknown";
                    string size = item["Size"] != null ? $"{Convert.ToInt64(item["Size"]) / (1024 * 1024 * 1024)}GB" : "Unknown";
                    string serial = item["SerialNumber"]?.ToString() ?? "Unknown";
                    info.Disks.Add($"{model} ({size}) SN:{serial}");
                }

                // GPU
                using var gpuSearcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion FROM Win32_VideoController");
                foreach (var item in gpuSearcher.Get())
                {
                    string name = item["Name"]?.ToString() ?? "Unknown GPU";
                    string ram = item["AdapterRAM"] != null ? $"{Convert.ToInt64(item["AdapterRAM"]) / (1024 * 1024)}MB" : "Unknown VRAM";
                    string driver = item["DriverVersion"]?.ToString() ?? "Unknown Driver";
                    info.GPU.Add($"{name} VRAM:{ram} Driver:{driver}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect hardware info");
            }
        }

        private void CollectMotherboardInfo(NodeSystemInfo info)
        {
            try
            {
                using var boardSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Product, SerialNumber FROM Win32_BaseBoard");
                foreach (var item in boardSearcher.Get())
                {
                    string manufacturer = item["Manufacturer"]?.ToString() ?? "Unknown";
                    string product = item["Product"]?.ToString() ?? "Unknown";
                    string serial = item["SerialNumber"]?.ToString() ?? "Unknown";
                    info.Motherboard = $"{manufacturer} {product} SN:{serial}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect motherboard info");
            }
        }

        private void CollectNetworkInfo(NodeSystemInfo info)
        {
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    var props = nic.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        info.IPAddresses.Add(addr.Address.ToString());
                    }
                    info.MACAddresses.Add(nic.GetPhysicalAddress().ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect network info");
            }
        }
    }

}
