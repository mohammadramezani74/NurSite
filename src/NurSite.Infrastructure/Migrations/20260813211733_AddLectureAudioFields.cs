using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureAudioFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lectures_LectureSeriesId",
                table: "Lectures");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Lectures",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AudioPath",
                table: "Lectures",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AddColumn<string>(
                name: "ExternalAudioUrl",
                table: "Lectures",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchText",
                table: "Lectures",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_LectureSeriesId_EpisodeNumber",
                table: "Lectures",
                columns: new[] { "LectureSeriesId", "EpisodeNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lectures_LectureSeriesId_EpisodeNumber",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "ExternalAudioUrl",
                table: "Lectures");

            migrationBuilder.DropColumn(
                name: "SearchText",
                table: "Lectures");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Lectures",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AudioPath",
                table: "Lectures",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_LectureSeriesId",
                table: "Lectures",
                column: "LectureSeriesId");
        }
    }
}
