using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConsoleApp4.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommandLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CommandName = table.Column<string>(type: "TEXT", nullable: false),
                    GuildName = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EconomyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<int>(type: "INTEGER", nullable: false),
                    Success = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomyTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectType = table.Column<int>(type: "INTEGER", nullable: false),
                    EffectValue = table.Column<int>(type: "INTEGER", nullable: false),
                    BuyPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    SellPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    IsConsumable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    Coins = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Items",
                columns: new[] { "Id", "BuyPrice", "Description", "EffectType", "EffectValue", "IsConsumable", "Name", "SellPrice", "Type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 120, "Recupera energia e da um bonus de EXP.", 2, 30, true, "Pocao", 60, 0 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 300, "Item raro que concede EXP adicional.", 2, 80, true, "Elixir", 150, 0 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 200, "Aumenta sua capacidade de carga (item de quest).", 0, 0, false, "Bolsa", 80, 2 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), 500, "Amuleto antigo usado em missoes.", 0, 0, false, "Amuleto", 250, 3 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 90, "Mapa para explorar novas areas.", 0, 0, false, "Mapa", 40, 2 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 160, "Item de venda rapida.", 1, 75, true, "MoedaDourada", 75, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommandLogs_ExecutedAtUtc",
                table: "CommandLogs",
                column: "ExecutedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyTransactions_CreatedAtUtc",
                table: "EconomyTransactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_DiscordGuildId",
                table: "Guilds",
                column: "DiscordGuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserItems_UserId_ItemId",
                table: "UserItems",
                columns: new[] { "UserId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordUserId",
                table: "Users",
                column: "DiscordUserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandLogs");

            migrationBuilder.DropTable(
                name: "EconomyTransactions");

            migrationBuilder.DropTable(
                name: "Guilds");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "UserItems");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
