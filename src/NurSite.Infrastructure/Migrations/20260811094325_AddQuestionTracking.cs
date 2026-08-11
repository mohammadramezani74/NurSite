using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowPublish",
                table: "UserQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SenderIpHash",
                table: "UserQuestions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingCode",
                table: "UserQuestions",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuestions_TrackingCode",
                table: "UserQuestions",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserQuestions_TrackingCode",
                table: "UserQuestions");

            migrationBuilder.DropColumn(
                name: "AllowPublish",
                table: "UserQuestions");

            migrationBuilder.DropColumn(
                name: "SenderIpHash",
                table: "UserQuestions");

            migrationBuilder.DropColumn(
                name: "TrackingCode",
                table: "UserQuestions");
        }
    }
}
