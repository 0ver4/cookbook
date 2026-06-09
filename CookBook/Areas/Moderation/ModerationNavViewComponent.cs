using CookBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Areas.Moderation;

public record ModerationNavVm(int PendingReports, int CategoryCount);

// Pasek zakładek panelu moderacji z licznikami (oczekujące zgłoszenia, liczba kategorii).
public class ModerationNavViewComponent(IReportService reports, ICategoryService categories) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var vm = new ModerationNavVm(
            await reports.GetPendingCountAsync(),
            (await categories.GetAllAsync()).Count);
        return View(vm);
    }
}
