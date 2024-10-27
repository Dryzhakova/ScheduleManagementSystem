using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class ClassesDescriptionCreateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassesDescription",
                columns: table => new
                {
                    ClassesDescriptionId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassesDescription", x => x.ClassesDescriptionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassesDescription");
        }
    }
}
