using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class UserGuildStats
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    public int MessageCount { get; set; }

    public int InviteCount { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
