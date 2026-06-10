using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Komplet statystyk per użytkownik; oparty na AspNetUsers, więc każdy user ma wiersz
            // (użytkownik bez treści: liczniki 0, średnie NULL).
            migrationBuilder.Sql(@"
CREATE VIEW vw_UserStats AS
SELECT
    u.Id AS UserId,
    (SELECT COUNT(*) FROM Recipes WHERE UserId = u.Id) AS RecipeCount,
    (SELECT COUNT(*) FROM Recipes WHERE UserId = u.Id AND IsPublished = 1 AND IsHidden = 0) AS PublishedRecipeCount,
    (SELECT COUNT(*) FROM Comments WHERE UserId = u.Id) AS CommentCount,
    (SELECT COUNT(*) FROM Reviews WHERE UserId = u.Id) AS ReviewCount,
    (SELECT AVG(CAST(Rating AS FLOAT)) FROM Reviews WHERE UserId = u.Id) AS AverageRatingGiven,
    (SELECT COUNT(*) FROM Collections WHERE UserId = u.Id) AS CollectionCount,
    (SELECT COUNT(*) FROM ShoppingLists WHERE UserId = u.Id) AS ShoppingListCount,
    (SELECT COUNT(*) FROM MealPlanItems WHERE UserId = u.Id) AS MealPlanItemCount,
    (SELECT AVG(CAST(rv.Rating AS FLOAT))
     FROM Reviews rv INNER JOIN Recipes r ON r.Id = rv.RecipeId
     WHERE r.UserId = u.Id) AS AverageRatingReceived
FROM AspNetUsers u;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_UserStats;");
        }
    }
}
