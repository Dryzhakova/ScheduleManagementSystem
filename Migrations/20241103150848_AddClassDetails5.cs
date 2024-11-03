using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddClassDetails5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecurringClasses_ClassesId",
                table: "RecurringClasses",
                column: "ClassesId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecurringClasses_Classes_ClassesId",
                table: "RecurringClasses",
                column: "ClassesId",
                principalTable: "Classes",
                principalColumn: "ClassesId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecurringClasses_Classes_ClassesId",
                table: "RecurringClasses");

            migrationBuilder.DropIndex(
                name: "IX_RecurringClasses_ClassesId",
                table: "RecurringClasses");
        }
    }
}
