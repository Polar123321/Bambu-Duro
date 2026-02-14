using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    public int Level { get; set; }

    public int Experience { get; set; }

    public int Coins { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
