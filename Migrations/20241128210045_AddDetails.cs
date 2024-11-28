using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Campus_CampusId",
                table: "Classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Campus",
                table: "Campus");

            migrationBuilder.RenameTable(
                name: "Campus",
                newName: "Campuses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Campuses",
                table: "Campuses",
                column: "Campusid");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Campuses_CampusId",
                table: "Classes",
                column: "CampusId",
                principalTable: "Campuses",
                principalColumn: "Campusid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_Campuses_CampusId",
                table: "Classes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Campuses",
                table: "Campuses");

            migrationBuilder.RenameTable(
                name: "Campuses",
                newName: "Campus");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Campus",
                table: "Campus",
                column: "Campusid");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_Campus_CampusId",
                table: "Classes",
                column: "CampusId",
                principalTable: "Campus",
                principalColumn: "Campusid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
