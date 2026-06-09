using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "Collections",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Collections_ApplicationUserId",
                table: "Collections",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_AspNetUsers_ApplicationUserId",
                table: "Collections",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Collections_AspNetUsers_ApplicationUserId",
                table: "Collections");

            migrationBuilder.DropIndex(
                name: "IX_Collections_ApplicationUserId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Collections");
        }
    }
}
