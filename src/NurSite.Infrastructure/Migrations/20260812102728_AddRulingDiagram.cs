using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NurSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRulingDiagram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasDiagram",
                table: "Rulings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RulingSourceId",
                table: "Rulings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourcePage",
                table: "Rulings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RulingNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RulingId = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulingNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RulingNodes_RulingNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RulingNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RulingNodes_Rulings_RulingId",
                        column: x => x.RulingId,
                        principalTable: "Rulings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RulingSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Editor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Publisher = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishedYear = table.Column<int>(type: "int", nullable: true),
                    Isbn = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Edition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CoverImagePath = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PermissionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulingSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RulingVerdicts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RulingNodeId = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SourceNote = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulingVerdicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RulingVerdicts_RulingNodes_RulingNodeId",
                        column: x => x.RulingNodeId,
                        principalTable: "RulingNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RulingVerdictMarjas",
                columns: table => new
                {
                    RulingVerdictId = table.Column<int>(type: "int", nullable: false),
                    MarjaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulingVerdictMarjas", x => new { x.RulingVerdictId, x.MarjaId });
                    table.ForeignKey(
                        name: "FK_RulingVerdictMarjas_Marjas_MarjaId",
                        column: x => x.MarjaId,
                        principalTable: "Marjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RulingVerdictMarjas_RulingVerdicts_RulingVerdictId",
                        column: x => x.RulingVerdictId,
                        principalTable: "RulingVerdicts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rulings_RulingSourceId",
                table: "Rulings",
                column: "RulingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_RulingNodes_ParentId",
                table: "RulingNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_RulingNodes_RulingId_ParentId_SortOrder",
                table: "RulingNodes",
                columns: new[] { "RulingId", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RulingSources_Slug",
                table: "RulingSources",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RulingVerdictMarjas_MarjaId",
                table: "RulingVerdictMarjas",
                column: "MarjaId");

            migrationBuilder.CreateIndex(
                name: "IX_RulingVerdicts_RulingNodeId_SortOrder",
                table: "RulingVerdicts",
                columns: new[] { "RulingNodeId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_Rulings_RulingSources_RulingSourceId",
                table: "Rulings",
                column: "RulingSourceId",
                principalTable: "RulingSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rulings_RulingSources_RulingSourceId",
                table: "Rulings");

            migrationBuilder.DropTable(
                name: "RulingSources");

            migrationBuilder.DropTable(
                name: "RulingVerdictMarjas");

            migrationBuilder.DropTable(
                name: "RulingVerdicts");

            migrationBuilder.DropTable(
                name: "RulingNodes");

            migrationBuilder.DropIndex(
                name: "IX_Rulings_RulingSourceId",
                table: "Rulings");

            migrationBuilder.DropColumn(
                name: "HasDiagram",
                table: "Rulings");

            migrationBuilder.DropColumn(
                name: "RulingSourceId",
                table: "Rulings");

            migrationBuilder.DropColumn(
                name: "SourcePage",
                table: "Rulings");
        }
    }
}
