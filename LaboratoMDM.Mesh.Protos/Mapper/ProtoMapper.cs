using LaboratoMDM.Core.Models.User;
using Google.Protobuf.WellKnownTypes;

namespace LaboratoMDM.Mesh.Protos.Mapper
{
    public static class NodeFullInfoMapper
    {
        #region NodeSystemInfo

        public static Laborato.Mesh.NodeSystemInfo ToProto(this Core.Models.NodeSystemInfo sys)
        {
            return new Laborato.Mesh.NodeSystemInfo
            {
                NodeId = sys.NodeId.ToString(),
                HostName = sys.HostName,
                OsVersion = sys.OSVersion,
                Cpu = sys.CPU,
                RamGb = sys.RAMGb
            }
            .AddDisks(sys.Disks)
            .AddGpu(sys.GPU)
            .AddIpAddresses(sys.IPAddresses)
            .AddMacAddresses(sys.MACAddresses)
            .WithMotherboard(sys.Motherboard);
        }

        public static Core.Models.NodeSystemInfo FromProto(this Laborato.Mesh.NodeSystemInfo proto)
        {
            return new Core.Models.NodeSystemInfo
            {
                NodeId = Guid.TryParse(proto.NodeId, out var id) ? id : Guid.NewGuid(),
                HostName = proto.HostName,
                OSVersion = proto.OsVersion,
                CPU = proto.Cpu,
                RAMGb = proto.RamGb,
                Disks = proto.Disks.ToList(),
                GPU = proto.Gpu.ToList(),
                IPAddresses = proto.IpAddresses.ToList(),
                MACAddresses = proto.MacAddresses.ToList(),
                Motherboard = proto.Motherboard
            };
        }

        #endregion

        #region UserInfo

        public static Laborato.Mesh.UserInfo ToProto(this Core.Models.User.UserInfo user)
        {
            return new Laborato.Mesh.UserInfo
            {
                Name = user.Name,
                Sid = user.Sid ?? "",
                AccountType = user.AccountType.ToString(),
                IsEnabled = user.IsEnabled,
                Description = user.Description ?? "",
                HomeDirectory = user.HomeDirectory ?? ""
            }
            .AddGroups(user.Groups);
        }

        public static Core.Models.User.UserInfo FromProto(this Laborato.Mesh.UserInfo proto)
        {
            return new Core.Models.User.UserInfo
            {
                Name = proto.Name,
                Sid = string.IsNullOrWhiteSpace(proto.Sid) ? null : proto.Sid,
                AccountType = System.Enum.TryParse<UserAccountType>(proto.AccountType, out var type) ? type : UserAccountType.Local,
                IsEnabled = proto.IsEnabled,
                Description = string.IsNullOrWhiteSpace(proto.Description) ? null : proto.Description,
                HomeDirectory = string.IsNullOrWhiteSpace(proto.HomeDirectory) ? null : proto.HomeDirectory,
                Groups = proto.Groups.ToArray()
            };
        }

        #endregion

        #region NodeFullInfo

        public static Laborato.Mesh.NodeFullInfo ToProto(this Core.Models.Node.NodeFullInfo node)
        {
            var proto = new Laborato.Mesh.NodeFullInfo
            {
                SystemInfo = node.SystemInfo.ToProto(),
                LastBootTime = Timestamp.FromDateTime(node.LastBootTime.ToUniversalTime()),
                Manufacturer = node.Manufacturer,
                Model = node.Model,
                FirmwareVersion = node.FirmwareVersion,
                TimeZone = node.TimeZone,
                IsDomainJoined = node.IsDomainJoined
            };

            proto.Users.AddRange(node.Users.Select(u => u.ToProto()));

            return proto;
        }

        public static Core.Models.Node.NodeFullInfo FromProto(this Laborato.Mesh.NodeFullInfo proto)
        {
            return new Core.Models.Node.NodeFullInfo
            {
                SystemInfo = proto.SystemInfo.FromProto(),
                Users = proto.Users.Select(u => u.FromProto()).ToList(),
                LastBootTime = proto.LastBootTime.ToDateTime(),
                Manufacturer = proto.Manufacturer,
                Model = proto.Model,
                FirmwareVersion = proto.FirmwareVersion,
                TimeZone = proto.TimeZone,
                IsDomainJoined = proto.IsDomainJoined
            };
        }

        #endregion

        #region Helpers for repeated fields

        private static Laborato.Mesh.NodeSystemInfo AddDisks(this Laborato.Mesh.NodeSystemInfo proto, IEnumerable<string> disks)
        {
            proto.Disks.AddRange(disks);
            return proto;
        }

        private static Laborato.Mesh.NodeSystemInfo AddGpu(this Laborato.Mesh.NodeSystemInfo proto, IEnumerable<string> gpu)
        {
            proto.Gpu.AddRange(gpu);
            return proto;
        }

        private static Laborato.Mesh.NodeSystemInfo AddIpAddresses(this Laborato.Mesh.NodeSystemInfo proto, IEnumerable<string> ips)
        {
            proto.IpAddresses.AddRange(ips);
            return proto;
        }

        private static Laborato.Mesh.NodeSystemInfo AddMacAddresses(this Laborato.Mesh.NodeSystemInfo proto, IEnumerable<string> macs)
        {
            proto.MacAddresses.AddRange(macs);
            return proto;
        }

        private static Laborato.Mesh.NodeSystemInfo WithMotherboard(this Laborato.Mesh.NodeSystemInfo proto, string mb)
        {
            proto.Motherboard = mb;
            return proto;
        }

        private static Laborato.Mesh.UserInfo AddGroups(this Laborato.Mesh.UserInfo proto, IEnumerable<string> groups)
        {
            proto.Groups.AddRange(groups);
            return proto;
        }

        #endregion
    }
}
