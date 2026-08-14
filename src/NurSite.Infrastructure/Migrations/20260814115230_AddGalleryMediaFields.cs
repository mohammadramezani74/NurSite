using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGalleryMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DownloadCount",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalVideoUrl",
                table: "Photos",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Photos",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Photos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Photos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "Photos",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Kind",
                table: "Photos",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Slug",
                table: "Photos",
                column: "Slug",
                unique: true);
        }
        //4EU7-VQX7
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_Kind",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Slug",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DownloadCount",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ExternalVideoUrl",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "Photos");
        }
    }
}
