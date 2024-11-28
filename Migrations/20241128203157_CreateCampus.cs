using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class CreateCampus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampusId",
                table: "Classes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Campus",
                columns: table => new
                {
                    Campusid = table.Column<string>(type: "TEXT", nullable: false),
                    CampusName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campus", x => x.Campusid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CampusId",
                table: "Classes",
                column: "CampusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Campus_CampusId",
                table: "Classes",
                column: "CampusId",
                principalTable: "Campus",
                principalColumn: "Campusid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Campus_CampusId",
                table: "Classes");

            migrationBuilder.DropTable(
                name: "Campus");

            migrationBuilder.DropIndex(
                name: "IX_Classes_CampusId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "CampusId",
                table: "Classes");
        }
    }
}
