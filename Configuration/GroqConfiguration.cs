using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class GroqConfiguration
{
    /// <summary>
    /// Groq API key. Prefer using environment variables, not committing this value to disk.
    /// Ex: DISCORD_BOT_Groq__ApiKey
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>
    /// Model name, e.g. "llama-3.3-70b-versatile".
    /// </summary>
    [Required]
    public string Model { get; init; } = "llama-3.3-70b-versatile";

    [Range(0.0, 2.0)]
    public double Temperature { get; init; } = 0.95;

    [Range(32, 2048)]
    public int MaxTokens { get; init; } = 220;
}
