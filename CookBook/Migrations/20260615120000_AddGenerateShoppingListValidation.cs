using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerateShoppingListValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dwie rzeczy:
            // 1) Walidacja reguły biznesowej - THROW 50001 (odpowiednik PL/SQL RAISE_APPLICATION_ERROR)
            //    przy przepisie bez składników; propaguje się jako SqlException do aplikacji.
            // 2) Upsert składników realizowany KURSOREM - iteracja po składnikach przepisu i dla
            //    każdego UPDATE (gdy pozycja istnieje) albo INSERT (gdy brak).
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE usp_GenerateShoppingList
    @shoppingListId INT,
    @recipeId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM RecipeIngredients WHERE RecipeId = @recipeId)
        THROW 50001, N'Przepis nie ma składników - nie można wygenerować listy zakupów.', 1;

    DECLARE @ingredientId INT, @unitId INT, @amount FLOAT;

    DECLARE ingredient_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT ri.IngredientId, COALESCE(ri.UnitId, i.UnitId), ri.Amount
        FROM RecipeIngredients ri
        JOIN Ingredients i ON i.Id = ri.IngredientId
        WHERE ri.RecipeId = @recipeId;

    OPEN ingredient_cursor;
    FETCH NEXT FROM ingredient_cursor INTO @ingredientId, @unitId, @amount;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Próba UPDATE; jeśli nic nie zaktualizowano (@@ROWCOUNT = 0), wstaw nową pozycję.
        UPDATE ShoppingListItems
            SET Amount = Amount + @amount
        WHERE ShoppingListId = @shoppingListId
          AND IngredientId   = @ingredientId
          AND UnitId         = @unitId;

        IF @@ROWCOUNT = 0
            INSERT INTO ShoppingListItems (ShoppingListId, IngredientId, Amount, IsChecked, UnitId)
            VALUES (@shoppingListId, @ingredientId, @amount, 0, @unitId);

        FETCH NEXT FROM ingredient_cursor INTO @ingredientId, @unitId, @amount;
    END

    CLOSE ingredient_cursor;
    DEALLOCATE ingredient_cursor;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Przywraca procedurę bez walidacji (stan sprzed tej migracji).
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE usp_GenerateShoppingList
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
    }
}
