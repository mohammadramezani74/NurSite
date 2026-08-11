using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHijriOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HijriDayOffset",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddCheckConstraint(
                name: "CK_SiteSetting_HijriOffset",
                table: "SiteSettings",
                sql: "[HijriDayOffset] BETWEEN -3 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_SiteSetting_HijriOffset",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HijriDayOffset",
                table: "SiteSettings");
        }
    }
}
