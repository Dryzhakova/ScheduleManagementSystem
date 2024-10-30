using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class AddClassDetails3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassesDescriptionId",
                table: "Classes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_ClassesDescriptionId",
                table: "Classes",
                column: "ClassesDescriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_ClassesDescription_ClassesDescriptionId",
                table: "Classes",
                column: "ClassesDescriptionId",
                principalTable: "ClassesDescription",
                principalColumn: "ClassesDescriptionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_ClassesDescription_ClassesDescriptionId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_ClassesDescriptionId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "ClassesDescriptionId",
                table: "Classes");
        }
    }
}
