using CookBook.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CookBook.Controllers;

[Authorize]
public class NotificationController(INotificationService service) : Controller
{
    private int CurrentUserId => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    public async Task<IActionResult> Index()
    {
        var notifications = await service.GetForUserAsync(CurrentUserId);
        return View(notifications);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await service.MarkAsReadAsync(id, CurrentUserId);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await service.MarkAllAsReadAsync(CurrentUserId);
        return RedirectToAction(nameof(Index));
    }
}
