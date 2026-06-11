using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeNutritionFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sumuje wartości odżywcze przepisu (przez fn_IngredientAmountInGrams).
            // Wszystkie kolumny NULL, gdy przepis nie jest policzalny: brak składników,
            // składnik bez przelicznika na gramy, albo bez kompletu 6 wartości odżywczych
            // (typy 1=Kalorie,2=Białko,3=Tłuszcze,4=Węglowodany,5=Błonnik,6=Cukry).
            migrationBuilder.Sql(@"
CREATE FUNCTION fn_RecipeNutrition(@recipeId INT)
RETURNS TABLE
AS
RETURN
(
    WITH ing AS (
        SELECT
            ri.IngredientId,
            dbo.fn_IngredientAmountInGrams(ri.IngredientId, ri.Amount, COALESCE(ri.UnitId, i.UnitId)) AS Grams,
            (SELECT COUNT(*) FROM IngredientNutritions n
             WHERE n.IngredientId = ri.IngredientId AND n.NutritionTypeId IN (1,2,3,4,5,6)) AS NutCount
        FROM RecipeIngredients ri
        JOIN Ingredients i ON i.Id = ri.IngredientId
        WHERE ri.RecipeId = @recipeId
    ),
    ok AS (
        SELECT CASE
            WHEN COUNT(*) > 0
             AND SUM(CASE WHEN Grams IS NULL OR NutCount < 6 THEN 1 ELSE 0 END) = 0
            THEN 1 ELSE 0 END AS Computable
        FROM ing
    )
    SELECT
        SUM(CASE WHEN n.NutritionTypeId = 1 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Calories,
        SUM(CASE WHEN n.NutritionTypeId = 2 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Protein,
        SUM(CASE WHEN n.NutritionTypeId = 3 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Fat,
        SUM(CASE WHEN n.NutritionTypeId = 4 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Carbs,
        SUM(CASE WHEN n.NutritionTypeId = 5 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Fiber,
        SUM(CASE WHEN n.NutritionTypeId = 6 THEN n.AmountPer100g * ig.Grams / 100.0 END) AS Sugar
    FROM ok
    CROSS JOIN ing ig
    JOIN IngredientNutritions n ON n.IngredientId = ig.IngredientId AND n.NutritionTypeId IN (1,2,3,4,5,6)
    WHERE ok.Computable = 1
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_RecipeNutrition;");
        }
    }
}
