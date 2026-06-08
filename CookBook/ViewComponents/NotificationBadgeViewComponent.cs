using CookBook.Services;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.ViewComponents;

// ViewComponent to kawałek logiki + widoku wywoływany z poziomu layoutu lub innego widoku,
// podobnie jak partial view, ale może korzystać z serwisów przez DI.
public class NotificationBadgeViewComponent(INotificationService service) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!UserClaimsPrincipal.Identity?.IsAuthenticated == true)
            return Content(string.Empty);

        var userId = int.Parse(UserClaimsPrincipal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var count = await service.GetUnreadCountAsync(userId);
        return View(count);
    }
}
