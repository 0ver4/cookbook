using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientConversionFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Przelicza ilość składnika na gramy wg jednostki i danych składnika.
            // Zwraca NULL, gdy brak danych do przeliczenia (np. mililitry bez gęstości).
            migrationBuilder.Sql(@"
CREATE FUNCTION fn_IngredientAmountInGrams(@ingredientId INT, @amount FLOAT, @unitId INT)
RETURNS FLOAT
AS
BEGIN
    DECLARE @density FLOAT, @perPiece FLOAT, @unitName NVARCHAR(100), @ml FLOAT;

    SELECT @density = DensityGramsPerMl, @perPiece = GramsPerPiece
    FROM Ingredients WHERE Id = @ingredientId;

    SET @unitName = (SELECT Name FROM Units WHERE Id = @unitId);

    -- Jednostki objętości -> mililitry (typowe miary kuchenne)
    SET @ml = CASE @unitName
        WHEN N'mililitr' THEN @amount
        WHEN N'szklanka' THEN @amount * 250.0
        WHEN N'łyżka'    THEN @amount * 15.0
        WHEN N'łyżeczka' THEN @amount * 5.0
        ELSE NULL
    END;

    RETURN CASE
        WHEN @unitName = N'gram'   THEN @amount
        WHEN @unitName = N'sztuka' THEN @amount * @perPiece   -- NULL jeśli brak gramatury
        WHEN @ml IS NOT NULL       THEN @ml * @density         -- NULL jeśli brak gęstości
        ELSE NULL
    END;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_IngredientAmountInGrams;");
        }
    }
}
