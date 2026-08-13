using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioKindAndDownloadAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDownload",
                table: "Lectures");

            migrationBuilder.AddColumn<int>(
                name: "DownloadAccess",
                table: "Lectures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Lectures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_Kind_Status_PublishedAtUtc",
                table: "Lectures",
                columns: new[] { "Kind", "Status", "PublishedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lectures_Kind_Status_PublishedAtUtc",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "DownloadAccess",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Lectures");

            migrationBuilder.AddColumn<bool>(
                name: "AllowDownload",
                table: "Lectures",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
