using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class DatabaseConfiguration
{
    [Required]
    public string Provider { get; init; } = "Sqlite";

    [Required]
    public string ConnectionString { get; init; } = "Data Source=bot.db";
}
