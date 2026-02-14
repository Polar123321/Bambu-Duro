using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class UserMemoryEntry
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    [Required]
    public ulong DiscordChannelId { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(1200)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(24)]
    public string MoralTag { get; set; } = "neutro";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
