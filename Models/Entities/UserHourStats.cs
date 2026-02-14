using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class UserHourStats
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong DiscordUserId { get; set; }

    [Required]
    public ulong DiscordGuildId { get; set; }

    
    
    
    
    public int HourOfWeek { get; set; }

    public int MessageCount { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

