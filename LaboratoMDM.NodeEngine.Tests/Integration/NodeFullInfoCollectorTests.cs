using LaboratoMDM.NodeEngine.Implementations;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.Versioning;

namespace LaboratoMDM.NodeEngine.Tests.Integration
{
    [SupportedOSPlatform("windows")]
    public class NodeFullInfoCollectorIntegrationTests
    {
        private readonly NodeFullInfoCollector _collector;

        public NodeFullInfoCollectorIntegrationTests()
        {
            var systemCollectorLoggerMock = new Mock<ILogger<NodeSystemInfoCollector>>();
            var systemCollector = new NodeSystemInfoCollector(systemCollectorLoggerMock.Object);

            var userCollectorLoggerMock = new Mock<ILogger<UserCollectorService>>();
            var userCollector = new UserCollectorService(userCollectorLoggerMock.Object);

            var nodeFullInfoCollectorLoggerMock = new Mock<ILogger<NodeFullInfoCollector>>();
            _collector = new NodeFullInfoCollector(systemCollector, userCollector, nodeFullInfoCollectorLoggerMock.Object);
        }

        [Fact]
        public void Collect_Should_Return_FullInfo_With_Users_And_SystemInfo()
        {
            var result = _collector.Collect();

            Assert.NotNull(result);
            Assert.NotNull(result.SystemInfo);
            Assert.False(string.IsNullOrWhiteSpace(result.SystemInfo.HostName));
            Assert.False(string.IsNullOrWhiteSpace(result.SystemInfo.CPU));
            Assert.True(result.SystemInfo.RAMGb > 0);
            Assert.NotEmpty(result.SystemInfo.Disks);
            Assert.NotEmpty(result.SystemInfo.GPU);

            Assert.NotNull(result.Users);
            Assert.NotEmpty(result.Users);

            var currentUser = Environment.UserName;
            Assert.Contains(result.Users, u => u.Name.Equals(currentUser, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Collect_Should_Return_Valid_HardwareInfo()
        {
            var result = _collector.Collect();

            Assert.False(string.IsNullOrWhiteSpace(result.SystemInfo.Motherboard));
            Assert.NotEmpty(result.SystemInfo.IPAddresses);
            Assert.NotEmpty(result.SystemInfo.MACAddresses);
        }

        [Fact]
        public void Collect_Should_Return_System_Metadata()
        {
            var result = _collector.Collect();

            Assert.True(result.LastBootTime > DateTime.MinValue);
            Assert.IsType<bool>(result.IsDomainJoined);
            Assert.False(string.IsNullOrWhiteSpace(result.Manufacturer));
            Assert.False(string.IsNullOrWhiteSpace(result.Model));
            Assert.False(string.IsNullOrWhiteSpace(result.FirmwareVersion));
            Assert.False(string.IsNullOrWhiteSpace(result.TimeZone));
        }
    }
}
