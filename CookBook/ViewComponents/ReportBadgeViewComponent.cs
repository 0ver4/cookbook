using CookBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.ViewComponents;

// Pokazuje liczbę oczekujących zgłoszeń przy linku do panelu moderacji.
// Renderuje cokolwiek tylko dla moderatorów.
public class ReportBadgeViewComponent(IReportService service) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!UserClaimsPrincipal.IsInRole("Moderator"))
            return Content(string.Empty);

        var count = await service.GetPendingCountAsync();
        return View(count);
    }
}
