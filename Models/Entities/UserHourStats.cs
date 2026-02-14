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

    /// <summary>
    /// Local hour-of-week bucket (0..167). 0 = Sunday 00:00-00:59.
    /// Why: used to estimate overlapping active hours without scraping message history.
    /// </summary>
    public int HourOfWeek { get; set; }

    public int MessageCount { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

