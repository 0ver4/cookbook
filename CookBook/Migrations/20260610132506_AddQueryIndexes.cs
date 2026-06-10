using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_MealPlanItems_UserId",
                table: "MealPlanItems");

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_Published_CreatedAt",
                table: "Recipes",
                column: "CreatedAt",
                filter: "[IsPublished] = 1 AND [IsHidden] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanItems_UserId_Date",
                table: "MealPlanItems",
                columns: new[] { "UserId", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Recipes_Published_CreatedAt",
                table: "Recipes");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_MealPlanItems_UserId_Date",
                table: "MealPlanItems");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanItems_UserId",
                table: "MealPlanItems",
                column: "UserId");
        }
    }
}
