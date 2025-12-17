namespace LaboratoMDM.Core.Models.User
{
    public enum UserAccountType
    {
        Local,
        System
    }

    public sealed class UserInfo
    {
        public string Name { get; init; } = string.Empty;
        public string? Sid { get; init; }
        public UserAccountType AccountType { get; init; }
        public bool IsEnabled { get; init; }
        public DateTime? LastLogon { get; init; }
        public string? Description { get; init; }
        public string? HomeDirectory { get; init; }
        public IReadOnlyList<string> Groups { get; init; } = Array.Empty<string>();
    }
}