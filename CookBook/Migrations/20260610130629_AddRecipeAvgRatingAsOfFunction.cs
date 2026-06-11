using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeAvgRatingAsOfFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Średnia ocena i liczba recenzji przepisu wg stanu z chwili @asOf (time-travel po
            // temporal Reviews). Wymaga, by Reviews było już system-versioned (migracja AddReviewHistory).
            migrationBuilder.Sql(@"
CREATE FUNCTION fn_RecipeAvgRatingAsOf(@recipeId INT, @asOf datetime2)
RETURNS TABLE
AS
RETURN
(
    SELECT AVG(CAST(rv.Rating AS FLOAT)) AS AverageRating,
           COUNT(*)                      AS ReviewCount
    FROM Reviews FOR SYSTEM_TIME AS OF @asOf AS rv
    WHERE rv.RecipeId = @recipeId
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_RecipeAvgRatingAsOf;");
        }
    }
}
