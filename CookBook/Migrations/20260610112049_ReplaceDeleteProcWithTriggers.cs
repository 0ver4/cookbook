using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDeleteProcWithTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Recipes_RecipeId",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Recipes_RecipeId",
                table: "Comments",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // --- Procedurę zastępujemy triggerami ---
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_DeleteComment;");

            // Kaskadowe usuwanie poddrzewa pojedynczego komentarza (FK ReplyToId jest NO ACTION).
            // DELETE wewnątrz triggera INSTEAD OF nie odpala go ponownie — rekurencja siedzi w CTE.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_Comments_DeleteCascade
ON Comments
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    WITH Tree AS (
        SELECT Id FROM deleted
        UNION ALL
        SELECT c.Id
        FROM Comments c
        INNER JOIN Tree t ON c.ReplyToId = t.Id
    )
    DELETE FROM Comments
    WHERE Id IN (SELECT Id FROM Tree)
    OPTION (MAXRECURSION 0);
END");

            // Usuwanie przepisu: najpierw komentarze (FK Comments->Recipes jest teraz NO ACTION),
            // potem sam przepis — reszta dzieci (Reviews, RecipeImages, ...) kaskaduje na poziomie FK.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_Recipes_DeleteCascadeComments
ON Recipes
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM Comments
    WHERE RecipeId IN (SELECT Id FROM deleted);

    DELETE FROM Recipes
    WHERE Id IN (SELECT Id FROM deleted);
END");

            // Powiadomienia o nowym komentarzu (zastępuje logikę z RecipeService).
            // Typ 2 (OdpowiedzNaKomentarz) -> autor komentarza-rodzica.
            // Typ 1 (NowyKomentarz)        -> autor przepisu.
            migrationBuilder.Sql(@"
CREATE TRIGGER trg_Comments_AfterInsert_Notify
ON Comments
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Notifications (UserId, NotificationTypeId, TriggeredByUserId, CommentId, IsRead, CreatedAt)
    SELECT parent.UserId, 2, i.UserId, i.Id, 0, SYSUTCDATETIME()
    FROM inserted i
    INNER JOIN Comments parent ON parent.Id = i.ReplyToId
    WHERE i.ReplyToId IS NOT NULL
      AND parent.UserId <> i.UserId;

    INSERT INTO Notifications (UserId, NotificationTypeId, TriggeredByUserId, CommentId, IsRead, CreatedAt)
    SELECT r.UserId, 1, i.UserId, i.Id, 0, SYSUTCDATETIME()
    FROM inserted i
    INNER JOIN Recipes r ON r.Id = i.RecipeId
    WHERE i.ReplyToId IS NULL
      AND r.UserId <> i.UserId;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Najpierw kasujemy triggery — IOD-trigger na Comments blokuje przywrócenie kaskadowego FK.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Comments_AfterInsert_Notify;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Recipes_DeleteCascadeComments;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_Comments_DeleteCascade;");

            // Odtwarzamy procedurę (wersja rekurencyjna — stan sprzed migracji).
            migrationBuilder.Sql(@"
CREATE PROCEDURE usp_DeleteComment
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
END");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Recipes_RecipeId",
                table: "Comments");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Recipes_RecipeId",
                table: "Comments",
                column: "RecipeId",
                principalTable: "Recipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
