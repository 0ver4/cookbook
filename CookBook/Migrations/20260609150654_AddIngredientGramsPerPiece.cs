using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CookBook.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientGramsPerPiece : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GramsPerPiece",
                table: "Ingredients",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GramsPerPiece",
                table: "Ingredients");
        }
    }
}
