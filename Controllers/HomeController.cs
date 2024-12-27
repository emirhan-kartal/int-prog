using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity.IsAuthenticated )
        {
            return RedirectToAction("Index", "Files");
        }
        return View();
    }
} 