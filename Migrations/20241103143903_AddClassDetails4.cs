using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddClassDetails4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OneTimeClasses_ClassesId",
                table: "OneTimeClasses",
                column: "ClassesId");

            migrationBuilder.AddForeignKey(
                name: "FK_OneTimeClasses_Classes_ClassesId",
                table: "OneTimeClasses",
                column: "ClassesId",
                principalTable: "Classes",
                principalColumn: "ClassesId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OneTimeClasses_Classes_ClassesId",
                table: "OneTimeClasses");

            migrationBuilder.DropIndex(
                name: "IX_OneTimeClasses_ClassesId",
                table: "OneTimeClasses");
        }
    }
}
