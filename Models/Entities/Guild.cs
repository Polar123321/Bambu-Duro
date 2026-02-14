using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class Guild
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
