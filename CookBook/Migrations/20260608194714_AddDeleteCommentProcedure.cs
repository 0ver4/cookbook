using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteCommentProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE PROCEDURE usp_DeleteComment
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS usp_DeleteComment;");
        }
    }
}
