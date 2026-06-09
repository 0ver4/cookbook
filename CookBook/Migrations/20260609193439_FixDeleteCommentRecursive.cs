using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class FixDeleteCommentRecursive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Poprzednia wersja usuwała tylko jeden poziom odpowiedzi, więc usunięcie
            // komentarza z zagnieżdżonymi odpowiedziami (głębokość >= 3) łamało FK Restrict.
            // Tutaj zbieramy całe poddrzewo rekurencyjnym CTE i kasujemy je jednym
            // zapytaniem zbiorczym - po jego wykonaniu żaden pozostały wiersz nie wskazuje
            // na usunięty, więc ograniczenie Restrict jest spełnione. Reakcje i zgłoszenia
            // komentarzy mają FK Cascade, więc usuwają się automatycznie.
            migrationBuilder.Sql(@"
ALTER PROCEDURE usp_DeleteComment
    @CommentId INT
AS
BEGIN
    SET NOCOUNT ON;

    WITH Tree AS (
        SELECT Id FROM Comments WHERE Id = @CommentId
        UNION ALL
        SELECT c.Id FROM Comments c
        INNER JOIN Tree t ON c.ReplyToId = t.Id
    )
    DELETE FROM Comments
    WHERE Id IN (SELECT Id FROM Tree)
    OPTION (MAXRECURSION 0);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER PROCEDURE usp_DeleteComment
    @CommentId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Najpierw usuń odpowiedzi (FK Restrict nie pozwala usunąć rodzica z odpowiedziami)
    DELETE FROM Comments WHERE ReplyToId = @CommentId;

    -- Potem usuń sam komentarz
    DELETE FROM Comments WHERE Id = @CommentId;
END
");
        }
    }
}
