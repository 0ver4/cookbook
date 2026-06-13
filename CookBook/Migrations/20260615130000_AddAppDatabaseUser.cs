using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddAppDatabaseUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tworzy ograniczonego użytkownika bazodanowego dla aplikacji (zasada least privilege).
            // Aplikacja łączy się w runtime jako 'cookbook_app' (connection string CookBookDb),
            // a migracje/DDL idą osobnym połączeniem administracyjnym (CookBookAdmin / sa).
            // Idempotentne (IF NOT EXISTS), bo login serwerowy przeżywa DROP DATABASE.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'cookbook_app')
    CREATE LOGIN cookbook_app WITH PASSWORD = 'CookbookApp!2026_Xq7';

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'cookbook_app')
    CREATE USER cookbook_app FOR LOGIN cookbook_app;

-- Role bazodanowe: odczyt + zapis danych (bez DDL).
ALTER ROLE db_datareader ADD MEMBER cookbook_app;
ALTER ROLE db_datawriter ADD MEMBER cookbook_app;

-- EXECUTE na procedurach i funkcjach skalarnych (np. usp_GenerateShoppingList, fn_IngredientAmountInGrams).
GRANT EXECUTE TO cookbook_app;

-- Inline TVF nie są objęte rolą db_datareader - trzeba nadać SELECT jawnie.
GRANT SELECT ON OBJECT::dbo.fn_RecipeNutrition TO cookbook_app;
GRANT SELECT ON OBJECT::dbo.fn_RecipeAvgRatingAsOf TO cookbook_app;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'cookbook_app')
    DROP USER cookbook_app;

IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'cookbook_app')
    DROP LOGIN cookbook_app;
");
        }
    }
}
