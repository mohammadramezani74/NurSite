using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHijriMonthStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HijriMonthStarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HijriYear = table.Column<int>(type: "int", nullable: false),
                    HijriMonth = table.Column<int>(type: "int", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HijriMonthStarts", x => x.Id);
                    table.CheckConstraint("CK_HijriMonthStart_Month", "[HijriMonth] BETWEEN 1 AND 12");
                    table.CheckConstraint("CK_HijriMonthStart_Year", "[HijriYear] BETWEEN 1300 AND 1600");
                });

            migrationBuilder.CreateIndex(
                name: "IX_HijriMonthStarts_HijriYear_HijriMonth",
                table: "HijriMonthStarts",
                columns: new[] { "HijriYear", "HijriMonth" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HijriMonthStarts");
        }
    }
}
