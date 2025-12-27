using LaboratoMDM.Mesh.Master.Grpc.Operator.V1;

namespace LaboratoMDM.UI.Operator.ViewModels;

public sealed class UserViewModel
{
    public UserViewModel(UserInfo info)
    {
        Name = info.Name;
        Sid = info.Sid;
        AccountType = info.AccountType.ToString();
        IsEnabled = info.IsEnabled;
        LastLogon = info.LastLogonUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(info.LastLogonUnix).DateTime
            : (DateTime?)null;

        Groups = string.Join(',',info.Groups);
    }

    public string Name { get; }
    public string Sid { get; }
    public string AccountType { get; }
    public bool IsEnabled { get; }
    public DateTime? LastLogon { get; }
    public string Groups { get; }
}
