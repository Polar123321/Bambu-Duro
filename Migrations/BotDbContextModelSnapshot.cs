
using System;
using ConsoleApp4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ConsoleApp4.Migrations
{
    [DbContext(typeof(BotDbContext))]
    partial class BotDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.13");

            modelBuilder.Entity("ConsoleApp4.Models.Entities.CommandLog", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("CommandName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ErrorMessage")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("ExecutedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("GuildName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Success")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ExecutedAtUtc");

                    b.ToTable("CommandLogs");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.EconomyTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Amount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Success")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAtUtc");

                    b.ToTable("EconomyTransactions");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.Guild", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DiscordGuildId")
                        .IsUnique();

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.Item", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("BuyPrice")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.Property<int>("EffectType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EffectValue")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsConsumable")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(80)
                        .HasColumnType("TEXT");

                    b.Property<int>("SellPrice")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Items");

                    b.HasData(
                        new
                        {
                            Id = new Guid("11111111-1111-1111-1111-111111111111"),
                            BuyPrice = 120,
                            Description = "Recupera energia e da um bonus de EXP.",
                            EffectType = 2,
                            EffectValue = 30,
                            IsConsumable = true,
                            Name = "Pocao",
                            SellPrice = 60,
                            Type = 0
                        },
                        new
                        {
                            Id = new Guid("22222222-2222-2222-2222-222222222222"),
                            BuyPrice = 300,
                            Description = "Item raro que concede EXP adicional.",
                            EffectType = 2,
                            EffectValue = 80,
                            IsConsumable = true,
                            Name = "Elixir",
                            SellPrice = 150,
                            Type = 0
                        },
                        new
                        {
                            Id = new Guid("33333333-3333-3333-3333-333333333333"),
                            BuyPrice = 200,
                            Description = "Aumenta sua capacidade de carga (item de quest).",
                            EffectType = 0,
                            EffectValue = 0,
                            IsConsumable = false,
                            Name = "Bolsa",
                            SellPrice = 80,
                            Type = 2
                        },
                        new
                        {
                            Id = new Guid("44444444-4444-4444-4444-444444444444"),
                            BuyPrice = 500,
                            Description = "Amuleto antigo usado em missoes.",
                            EffectType = 0,
                            EffectValue = 0,
                            IsConsumable = false,
                            Name = "Amuleto",
                            SellPrice = 250,
                            Type = 3
                        },
                        new
                        {
                            Id = new Guid("55555555-5555-5555-5555-555555555555"),
                            BuyPrice = 90,
                            Description = "Mapa para explorar novas areas.",
                            EffectType = 0,
                            EffectValue = 0,
                            IsConsumable = false,
                            Name = "Mapa",
                            SellPrice = 40,
                            Type = 2
                        },
                        new
                        {
                            Id = new Guid("66666666-6666-6666-6666-666666666666"),
                            BuyPrice = 160,
                            Description = "Item de venda rapida.",
                            EffectType = 1,
                            EffectValue = 75,
                            IsConsumable = true,
                            Name = "MoedaDourada",
                            SellPrice = 75,
                            Type = 3
                        });
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Coins")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedAtUtc")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Experience")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Level")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DiscordUserId")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.UserChannelStats", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("DiscordGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("DiscordUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DiscordUserId", "DiscordGuildId", "DiscordChannelId")
                        .IsUnique();

                    b.ToTable("UserChannelStats");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.UserGuildStats", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("DiscordUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("InviteCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DiscordUserId", "DiscordGuildId")
                        .IsUnique();

                    b.ToTable("UserGuildStats");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.UserHourStats", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<ulong>("DiscordGuildId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("DiscordUserId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("HourOfWeek")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UpdatedAtUtc")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("DiscordUserId", "DiscordGuildId", "HourOfWeek")
                        .IsUnique();

                    b.ToTable("UserHourStats");
                });

            modelBuilder.Entity("ConsoleApp4.Models.Entities.UserItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ItemId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Quantity")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("UserId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserId", "ItemId")
                        .IsUnique();

                    b.ToTable("UserItems");
                });
#pragma warning restore 612, 618
        }
    }
}
