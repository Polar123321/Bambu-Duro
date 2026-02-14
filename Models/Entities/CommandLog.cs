using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Models.Entities;

public sealed class CommandLog
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public ulong UserId { get; set; }

    [Required]
    public string CommandName { get; set; } = string.Empty;

    public string? GuildName { get; set; }

    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }
}
