using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class OneTimeClassDateCreateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OneTimeClasses",
                columns: table => new
                {
                    OneTimeClassDateId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassesId = table.Column<string>(type: "TEXT", nullable: false),
                    OneTimeClassFullDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OneTimeClassStartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OneTimeClassEndTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeClasses", x => x.OneTimeClassDateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OneTimeClasses");
        }
    }
}
