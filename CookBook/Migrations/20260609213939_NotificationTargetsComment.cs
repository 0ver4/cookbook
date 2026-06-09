using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class NotificationTargetsComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stare powiadomienia celowały w przepis (CommentId NULL) i nie da się ich
            // zmapować na komentarz — czyścimy, zanim CommentId stanie się NOT NULL.
            migrationBuilder.Sql("DELETE FROM Notifications;");

            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Comments_CommentId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Recipes_RecipeId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeToCollections_Recipes_RecipeId",
                table: "RecipeToCollections");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipeId",
                table: "Notifications");

            migrationBuilder.DropCheckConstraint(
                name: "chk_notification_one_target",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RecipeId",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "CommentId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Comments_CommentId",
                table: "Notifications",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeToCollections_Recipes_RecipeId",
                table: "RecipeToCollections",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Comments_CommentId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_RecipeToCollections_Recipes_RecipeId",
                table: "RecipeToCollections");

            migrationBuilder.AlterColumn<int>(
                name: "CommentId",
                table: "Notifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "RecipeId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipeId",
                table: "Notifications",
                column: "RecipeId");

            migrationBuilder.AddCheckConstraint(
                name: "chk_notification_one_target",
                table: "Notifications",
                sql: "(CASE WHEN [RecipeId] IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN [CommentId] IS NOT NULL THEN 1 ELSE 0 END) <= 1");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanItems_Recipes_RecipeId",
                table: "MealPlanItems",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Comments_CommentId",
                table: "Notifications",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Recipes_RecipeId",
                table: "Notifications",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeToCollections_Recipes_RecipeId",
                table: "RecipeToCollections",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
