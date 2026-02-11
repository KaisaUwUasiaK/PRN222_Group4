using Microsoft.AspNetCore.Mvc;

namespace PRN222_Group4.Controllers;

public class AuthenticationController : Controller
{
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }
}
