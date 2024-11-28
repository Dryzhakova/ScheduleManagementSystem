using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class CanceledRecurringClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CanceledRecurringClasses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClassesId = table.Column<string>(type: "TEXT", nullable: false),
                    CanceledDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanceledRecurringClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanceledRecurringClasses_Classes_ClassesId",
                        column: x => x.ClassesId,
                        principalTable: "Classes",
                        principalColumn: "ClassesId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanceledRecurringClasses_ClassesId",
                table: "CanceledRecurringClasses",
                column: "ClassesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanceledRecurringClasses");

            migrationBuilder.DropIndex(
              name: "IX_CanceledRecurringClasses_ClassesId",
              table: "CanceledRecurringClasses");
        }
    }
}
