using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeRatingsView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Średnia ocena i liczba recenzji per przepis.
            // LEFT JOIN: przepisy bez recenzji mają ReviewCount=0 i AverageRating=NULL.
            migrationBuilder.Sql(@"
CREATE VIEW vw_RecipeRatings AS
SELECT
    r.Id                          AS RecipeId,
    COUNT(rv.Rating)              AS ReviewCount,
    AVG(CAST(rv.Rating AS FLOAT)) AS AverageRating
FROM Recipes r
LEFT JOIN Reviews rv ON rv.RecipeId = r.Id
GROUP BY r.Id;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_RecipeRatings;");
        }
    }
}
