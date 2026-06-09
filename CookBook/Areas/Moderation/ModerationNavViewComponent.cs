using CookBook.Models;
using CookBook.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CookBook.Areas.Moderation;

public record ModerationNavTab(string Slug, string Label, int Count);
public record ModerationNavVm(int PendingReports, IReadOnlyList<ModerationNavTab> Dictionaries);

// Pasek zakładek panelu moderacji. Zakładki słowników generują się z LookupRegistry,
// licznik zgłoszeń liczy oczekujące zgłoszenia.
public class ModerationNavViewComponent : ViewComponent
{
    private readonly IReportService _reports;
    private readonly IServiceProvider _services;

    public ModerationNavViewComponent(IReportService reports, IServiceProvider services)
    {
        _reports = reports;
        _services = services;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var tabs = new List<ModerationNavTab>();
        foreach (var d in LookupRegistry.All)
        {
            var ops = (ILookupOps)_services
                .GetRequiredService(typeof(LookupService<>).MakeGenericType(d.EntityType));
            tabs.Add(new ModerationNavTab(d.Slug, $"{d.Emoji} {d.Plural}", await ops.CountAsync()));
        }

        return View(new ModerationNavVm(await _reports.GetPendingCountAsync(), tabs));
    }
}
