using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class ShoppingListItemUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingListItems",
                table: "ShoppingListItems");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "ShoppingListItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingListItems",
                table: "ShoppingListItems",
                columns: new[] { "ShoppingListId", "IngredientId", "UnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingListItems_UnitId",
                table: "ShoppingListItems",
                column: "UnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingListItems_Units_UnitId",
                table: "ShoppingListItems",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingListItems_Units_UnitId",
                table: "ShoppingListItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingListItems",
                table: "ShoppingListItems");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingListItems_UnitId",
                table: "ShoppingListItems");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "ShoppingListItems");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingListItems",
                table: "ShoppingListItems",
                columns: new[] { "ShoppingListId", "IngredientId" });
        }
    }
}
