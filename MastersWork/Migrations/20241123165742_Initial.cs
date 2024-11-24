using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MastersWork.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotCreationDatas",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    BotName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: true),
                    IsBotWorking = table.Column<bool>(type: "bit", nullable: true),
                    QA = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotCreationDatas", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "UserStates",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    TempData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStates", x => x.ChatId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotCreationDatas");

            migrationBuilder.DropTable(
                name: "UserStates");
        }
    }
}
