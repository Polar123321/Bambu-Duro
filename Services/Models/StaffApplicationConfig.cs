namespace ConsoleApp4.Services.Models;

public sealed class StaffApplicationConfig
{
    public ulong ChannelId { get; set; }
    public List<ulong> BannedUserIds { get; set; } = new();
    public List<ulong> RoleIds { get; set; } = new();
    public List<string> GlobalQuestions { get; set; } = new();
    public Dictionary<ulong, List<string>> RoleQuestions { get; set; } = new();
}
