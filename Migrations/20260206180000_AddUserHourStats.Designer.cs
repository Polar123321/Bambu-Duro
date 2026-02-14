
using System;
using ConsoleApp4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ConsoleApp4.Migrations
{
    [DbContext(typeof(BotDbContext))]
    [Migration("20260206180000_AddUserHourStats")]
    partial class AddUserHourStats
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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
#pragma warning restore 612, 618
        }
    }
}
