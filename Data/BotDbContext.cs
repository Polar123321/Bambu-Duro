using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Models.Enums;

namespace ConsoleApp4.Data;

public sealed class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<CommandLog> CommandLogs => Set<CommandLog>();
    public DbSet<UserGuildStats> UserGuildStats => Set<UserGuildStats>();
    public DbSet<UserChannelStats> UserChannelStats => Set<UserChannelStats>();
    public DbSet<UserHourStats> UserHourStats => Set<UserHourStats>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<UserItem> UserItems => Set<UserItem>();
    public DbSet<EconomyTransaction> EconomyTransactions => Set<EconomyTransaction>();
    public DbSet<WarnEntry> WarnEntries => Set<WarnEntry>();
    public DbSet<UserMemoryEntry> UserMemoryEntries => Set<UserMemoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.DiscordUserId)
            .IsUnique();

        modelBuilder.Entity<Guild>()
            .HasIndex(g => g.DiscordGuildId)
            .IsUnique();

        modelBuilder.Entity<CommandLog>()
            .HasIndex(c => c.ExecutedAtUtc);

        modelBuilder.Entity<UserItem>()
            .HasIndex(ui => new { ui.UserId, ui.ItemId })
            .IsUnique();

        modelBuilder.Entity<UserGuildStats>()
            .HasIndex(s => new { s.DiscordUserId, s.DiscordGuildId })
            .IsUnique();

        modelBuilder.Entity<UserChannelStats>()
            .HasIndex(s => new { s.DiscordUserId, s.DiscordGuildId, s.DiscordChannelId })
            .IsUnique();

        modelBuilder.Entity<UserHourStats>()
            .HasIndex(s => new { s.DiscordUserId, s.DiscordGuildId, s.HourOfWeek })
            .IsUnique();

        modelBuilder.Entity<EconomyTransaction>()
            .HasIndex(t => t.CreatedAtUtc);

        modelBuilder.Entity<WarnEntry>()
            .HasIndex(w => new { w.DiscordGuildId, w.DiscordUserId, w.RevokedAtUtc });

        modelBuilder.Entity<WarnEntry>()
            .HasIndex(w => w.CreatedAtUtc);

        modelBuilder.Entity<UserMemoryEntry>()
            .HasIndex(m => new { m.DiscordGuildId, m.DiscordUserId, m.CreatedAtUtc });

        modelBuilder.Entity<UserMemoryEntry>()
            .HasIndex(m => m.CreatedAtUtc);

        SeedItems(modelBuilder);
    }

    private static void SeedItems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>().HasData(
            new Item
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Pocao",
                Description = "Recupera energia e da um bonus de EXP.",
                Type = ItemType.Consumable,
                EffectType = ItemEffectType.AddExperience,
                EffectValue = 30,
                BuyPrice = 120,
                SellPrice = 60,
                IsConsumable = true
            },
            new Item
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Elixir",
                Description = "Item raro que concede EXP adicional.",
                Type = ItemType.Consumable,
                EffectType = ItemEffectType.AddExperience,
                EffectValue = 80,
                BuyPrice = 300,
                SellPrice = 150,
                IsConsumable = true
            },
            new Item
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Bolsa",
                Description = "Aumenta sua capacidade de carga (item de quest).",
                Type = ItemType.Quest,
                EffectType = ItemEffectType.None,
                EffectValue = 0,
                BuyPrice = 200,
                SellPrice = 80,
                IsConsumable = false
            },
            new Item
            {
                Id = new Guid("44444444-4444-4444-4444-444444444444"),
                Name = "Amuleto",
                Description = "Amuleto antigo usado em missoes.",
                Type = ItemType.Misc,
                EffectType = ItemEffectType.None,
                EffectValue = 0,
                BuyPrice = 500,
                SellPrice = 250,
                IsConsumable = false
            },
            new Item
            {
                Id = new Guid("55555555-5555-5555-5555-555555555555"),
                Name = "Mapa",
                Description = "Mapa para explorar novas areas.",
                Type = ItemType.Quest,
                EffectType = ItemEffectType.None,
                EffectValue = 0,
                BuyPrice = 90,
                SellPrice = 40,
                IsConsumable = false
            },
            new Item
            {
                Id = new Guid("66666666-6666-6666-6666-666666666666"),
                Name = "MoedaDourada",
                Description = "Item de venda rapida.",
                Type = ItemType.Misc,
                EffectType = ItemEffectType.AddCoins,
                EffectValue = 75,
                BuyPrice = 160,
                SellPrice = 75,
                IsConsumable = true
            }
        );
    }
}
