using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Data;

public sealed class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables(prefix: "DISCORD_BOT_")
            .Build();

        var dbConfig = config.GetSection("Database").Get<DatabaseConfiguration>()
                       ?? new DatabaseConfiguration();

        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        optionsBuilder.UseSqlite(dbConfig.ConnectionString);

        return new BotDbContext(optionsBuilder.Options);
    }
}
