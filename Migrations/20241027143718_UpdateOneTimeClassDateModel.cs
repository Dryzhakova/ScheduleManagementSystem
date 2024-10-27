using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppsMoodle.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOneTimeClassDateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удалите старую таблицу, если она существует
            migrationBuilder.DropTable(
                name: "OneTimeClasses");

            // Создайте новую таблицу с измененными типами данных
            migrationBuilder.CreateTable(
                name: "OneTimeClasses",
                columns: table => new
                {
                    OneTimeClassDateId = table.Column<string>(type: "TEXT", nullable: false),
                    ClassesId = table.Column<string>(type: "TEXT", nullable: false),
                    OneTimeClassFullDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OneTimeClassStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    OneTimeClassEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimeClasses", x => x.OneTimeClassDateId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удалите новую таблицу и восстановите старую
            migrationBuilder.DropTable(
                name: "OneTimeClasses");

            // Создайте предыдущую версию таблицы с типами данных
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
    }
}
