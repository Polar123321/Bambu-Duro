using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ConsoleApp4.Configuration;
using ConsoleApp4.Data;
using ConsoleApp4.Handlers;
using ConsoleApp4.Services;
using ConsoleApp4.Services.Interfaces;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

namespace ConsoleApp4;

internal static class BotHost
{
    public static IHost Build(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true,
                    reloadOnChange: true);
                config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true,
                    reloadOnChange: true);
                config.AddJsonFile(Path.Combine(AppContext.BaseDirectory,
                    $"appsettings.{context.HostingEnvironment.EnvironmentName}.json"), optional: true,
                    reloadOnChange: true);
                config.AddEnvironmentVariables(prefix: "DISCORD_BOT_");
            })
            .UseSerilog((context, services, logger) =>
            {
                logger.ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext();
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions<BotConfiguration>()
                    .Bind(context.Configuration.GetSection("Bot"))
                    .ValidateDataAnnotations();

                services.AddOptions<DatabaseConfiguration>()
                    .Bind(context.Configuration.GetSection("Database"))
                    .ValidateDataAnnotations();

                services.AddOptions<EconomyConfiguration>()
                    .Bind(context.Configuration.GetSection("Economy"))
                    .ValidateDataAnnotations();

                services.AddOptions<GroqConfiguration>()
                    .Bind(context.Configuration.GetSection("Groq"))
                    .ValidateDataAnnotations();

                services.AddOptions<AutoNoticeConfiguration>()
                    .Bind(context.Configuration.GetSection("AutoNotice"))
                    .ValidateDataAnnotations();

                services.AddOptions<BrainConfiguration>()
                    .Bind(context.Configuration.GetSection("Brain"))
                    .ValidateDataAnnotations();

                services.AddDbContext<BotDbContext>(options =>
                {
                    var dbConfig = context.Configuration.GetSection("Database").Get<DatabaseConfiguration>()
                                   ?? new DatabaseConfiguration();
                    var resolvedConnectionString = ResolveSqliteConnectionString(dbConfig.ConnectionString);

                    
                    
                    options.UseSqlite(resolvedConnectionString);
                });

                services.AddMemoryCache();
                services.AddHttpClient<WaifuPicsClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(6);
                });
                services.AddHttpClient<PinterestImageSearchService>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                });

                services.AddHttpClient<IGroqChatService, GroqChatService>(client =>
                {
                    client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
                    client.Timeout = TimeSpan.FromSeconds(20);
                });

                services.AddSingleton(new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged |
                                     GatewayIntents.MessageContent |
                                     GatewayIntents.GuildMembers,
                    AlwaysDownloadUsers = false,
                    LogLevel = LogSeverity.Info
                });

                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton(new CommandService(new CommandServiceConfig
                {
                    CaseSensitiveCommands = false,
                    DefaultRunMode = Discord.Commands.RunMode.Async
                }));
                services.AddSingleton(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));

                services.AddSingleton<IRateLimitService, RateLimitService>();
                services.AddSingleton<IModerationActionStore, ModerationActionStore>();
                services.AddSingleton<IStaffApplicationStore, JsonStaffApplicationStore>();
                services.AddSingleton<IGuildConfigStore, JsonGuildConfigStore>();
                services.AddSingleton<IMarriageStore, JsonMarriageStore>();
                services.AddSingleton<IShipStore, JsonShipStore>();
                services.AddSingleton<ILongTermMemoryStore, JsonLongTermMemoryStore>();
                services.AddScoped<IShipCompatibilityService, ShipCompatibilityService>();
                services.AddScoped<ICommandLogService, CommandLogService>();
                services.AddScoped<IUserService, UserService>();
                services.AddScoped<IGuildService, GuildService>();
                services.AddScoped<IEconomyService, EconomyService>();
                services.AddScoped<IUserGuildStatsService, UserGuildStatsService>();
                services.AddScoped<IUserHourStatsService, UserHourStatsService>();
                services.AddScoped<IUserMemoryService, UserMemoryService>();
                services.AddScoped<IWarnService, WarnService>();

                services.AddSingleton<Helpers.EmbedHelper>();

                services.AddSingleton<CommandHandler>();
                services.AddSingleton<InteractionHandler>();
                services.AddSingleton<WelcomeHandler>();
                services.AddSingleton<InviteTrackingHandler>();
                services.AddSingleton<BotClient>();
            })
            .Build();
    }

    public static async Task StartAsync(IHost host, CancellationToken cancellationToken = default)
    {
        
        await host.StartAsync(cancellationToken).ConfigureAwait(false);

        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            
            
            var hasMigrations = db.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await db.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            }

            
            
            await EnsureWarnEntriesSchemaAsync(db, cancellationToken).ConfigureAwait(false);
            await EnsureUserMemorySchemaAsync(db, cancellationToken).ConfigureAwait(false);
        }

        var bot = host.Services.GetRequiredService<BotClient>();
        await bot.StartAsync().ConfigureAwait(false);
    }

    public static async Task StopAsync(IHost host, CancellationToken cancellationToken = default)
    {
        var bot = host.Services.GetRequiredService<BotClient>();
        await bot.StopAsync().ConfigureAwait(false);
        await host.StopAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureWarnEntriesSchemaAsync(BotDbContext db, CancellationToken cancellationToken)
    {
        
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS WarnEntries (
                Id TEXT NOT NULL CONSTRAINT PK_WarnEntries PRIMARY KEY,
                DiscordGuildId INTEGER NOT NULL,
                DiscordUserId INTEGER NOT NULL,
                DiscordModeratorId INTEGER NOT NULL,
                Reason TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                RevokedAtUtc TEXT NULL,
                RevokedById INTEGER NULL
            );
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS IX_WarnEntries_DiscordGuildId_DiscordUserId_RevokedAtUtc
            ON WarnEntries (DiscordGuildId, DiscordUserId, RevokedAtUtc);
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS IX_WarnEntries_CreatedAtUtc
            ON WarnEntries (CreatedAtUtc);
            """, cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureUserMemorySchemaAsync(BotDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS UserMemoryEntries (
                Id TEXT NOT NULL CONSTRAINT PK_UserMemoryEntries PRIMARY KEY,
                DiscordGuildId INTEGER NOT NULL,
                DiscordChannelId INTEGER NOT NULL,
                DiscordUserId INTEGER NOT NULL,
                Username TEXT NOT NULL,
                Content TEXT NOT NULL,
                MoralTag TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS IX_UserMemoryEntries_DiscordGuildId_DiscordUserId_CreatedAtUtc
            ON UserMemoryEntries (DiscordGuildId, DiscordUserId, CreatedAtUtc);
            """, cancellationToken).ConfigureAwait(false);

        await db.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX IF NOT EXISTS IX_UserMemoryEntries_CreatedAtUtc
            ON UserMemoryEntries (CreatedAtUtc);
            """, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveSqliteConnectionString(string? configuredConnectionString)
    {
        var fallbackDataSource = Path.Combine(GetStableDataDirectory(), "bot.db");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return $"Data Source={fallbackDataSource}";
        }

        SqliteConnectionStringBuilder builder;
        try
        {
            builder = new SqliteConnectionStringBuilder(configuredConnectionString);
        }
        catch
        {
            return configuredConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(builder.DataSource) &&
            !string.Equals(builder.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase) &&
            !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.Combine(GetStableDataDirectory(), builder.DataSource);
        }

        return builder.ToString();
    }

    private static string GetStableDataDirectory()
    {
        var baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ConsoleApp4");
        Directory.CreateDirectory(baseDir);
        return baseDir;
    }
}
