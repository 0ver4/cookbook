using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerateShoppingListProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dodaje składniki przepisu do listy zakupów. Łączy po (składnik, jednostka):
            // istniejąca pozycja -> zwiększa Amount; brak -> nowa pozycja (IsChecked=0).
            // Jednostka jak w aplikacji: COALESCE(ri.UnitId, ingredient.UnitId). Bez konwersji jednostek.
            migrationBuilder.Sql(@"
CREATE PROCEDURE usp_GenerateShoppingList
    @shoppingListId INT,
    @recipeId INT
AS
BEGIN
    SET NOCOUNT ON;

    MERGE ShoppingListItems AS tgt
    USING (
        SELECT @shoppingListId        AS ShoppingListId,
               ri.IngredientId,
               COALESCE(ri.UnitId, i.UnitId) AS UnitId,
               ri.Amount
        FROM RecipeIngredients ri
        JOIN Ingredients i ON i.Id = ri.IngredientId
        WHERE ri.RecipeId = @recipeId
    ) AS src
    ON  tgt.ShoppingListId = src.ShoppingListId
    AND tgt.IngredientId   = src.IngredientId
    AND tgt.UnitId         = src.UnitId
    WHEN MATCHED THEN
        UPDATE SET tgt.Amount = tgt.Amount + src.Amount
    WHEN NOT MATCHED THEN
        INSERT (ShoppingListId, IngredientId, Amount, IsChecked, UnitId)
        VALUES (src.ShoppingListId, src.IngredientId, src.Amount, 0, src.UnitId);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_GenerateShoppingList;");
        }
    }
}
