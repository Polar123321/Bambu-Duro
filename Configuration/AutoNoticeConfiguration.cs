using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class AutoNoticeConfiguration
{
    public ulong RoleId { get; init; }

    
    public List<ulong> UserIds { get; init; } = new();

    [Range(1, 3600)]
    public int CooldownSeconds { get; init; } = 8;

    [MaxLength(2000)]
    public string Message { get; init; } = "Porfavor, não ligue pra quaisquer piada que este indevido mandar neste chat.";
}
