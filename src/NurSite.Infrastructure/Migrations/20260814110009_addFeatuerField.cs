using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addFeatuerField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Lectures",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_IsFeatured",
                table: "Lectures",
                column: "IsFeatured");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lectures_IsFeatured",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Lectures");
        }
    }
}
