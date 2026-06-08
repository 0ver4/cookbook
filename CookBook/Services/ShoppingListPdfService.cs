using CookBook.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CookBook.Services;

public class ShoppingListPdfService : IShoppingListPdfService
{
    public byte[] Generate(ShoppingListDetailsDto list)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(list.Name).FontSize(20).Bold();
                    col.Item().Text($"Lista zakupów • {list.CreatedAt.ToLocalTime():dd.MM.yyyy}")
                        .FontSize(10).FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(25);   // checkbox
                        columns.RelativeColumn();      // nazwa
                        columns.ConstantColumn(120);   // ilość
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("");
                        header.Cell().Element(HeaderCell).Text("Składnik");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Ilość");
                    });

                    if (list.Items.Count == 0)
                    {
                        table.Cell().ColumnSpan(3).PaddingVertical(10)
                            .Text("Lista jest pusta.").FontColor(Colors.Grey.Medium).Italic();
                    }

                    foreach (var item in list.Items)
                    {
                        table.Cell().Element(BodyCell).Text(item.IsChecked ? "[x]" : "[ ]");
                        table.Cell().Element(BodyCell).Text(item.IngredientName);
                        table.Cell().Element(BodyCell).AlignRight()
                            .Text($"{item.Amount:0.##} {item.UnitName}");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("CookBook • strona ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(5)
            .DefaultTextStyle(x => x.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(6);
}
