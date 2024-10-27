using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class RecurringClassDateDateCreateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringClasses",
                columns: table => new
                {
                    RecurringClassDateId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassesId = table.Column<string>(type: "TEXT", nullable: false),
                    IsEveryWeek = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEven = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecurrenceDay = table.Column<int>(type: "INTEGER", nullable: false),
                    RecurrenceStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    RecurrenceEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringClasses", x => x.RecurringClassDateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringClasses");
        }
    }
}
