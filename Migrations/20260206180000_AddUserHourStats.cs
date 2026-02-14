using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConsoleApp4.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHourStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserHourStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DiscordUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    DiscordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    HourOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageCount = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserHourStats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserHourStats_DiscordUserId_DiscordGuildId_HourOfWeek",
                table: "UserHourStats",
                columns: new[] { "DiscordUserId", "DiscordGuildId", "HourOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserHourStats");
        }
    }
}

