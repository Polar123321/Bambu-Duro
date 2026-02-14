using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleApp4.Migrations
{
    
    public partial class AddUserChannelStats : Migration
    {
        
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserChannelStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserChannelStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserChannelStats_DiscordUserId_DiscordGuildId_DiscordChannelId",
                table: "UserChannelStats",
                columns: new[] { "DiscordUserId", "DiscordGuildId", "DiscordChannelId" },
                unique: true);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserChannelStats");
        }
    }
}
