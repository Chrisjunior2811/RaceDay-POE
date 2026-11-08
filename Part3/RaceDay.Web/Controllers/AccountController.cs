using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. Authentication itself is done by the Part 2 API.</summary>
public class AccountController : BaseController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? reason = null)
    {
        if (HttpContext.Session.IsAuthenticated())
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Notice"] = reason == "login" ? "Please log in to continue." : null;
        return View(new LoginViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password });
        if (!response.Success || response.Data is null)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not log you in. Please try again.");
            model.Password = string.Empty;
            return View(model);
        }

        // Start a fresh session holding the API token, role and name
        HttpContext.Session.SignIn(response.Data);
        FlashSuccess($"Welcome back, {response.Data.User.FullName}!");

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsAuthenticated())
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (model.Role == Roles.Organiser && string.IsNullOrWhiteSpace(model.OrganiserCode))
        {
            ModelState.AddModelError(nameof(model.OrganiserCode), "Organisers need the organiser registration code.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await Api.RegisterAsync(new RegisterRequest
        {
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            Role = model.Role,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            OrganiserCode = model.Role == Roles.Organiser ? model.OrganiserCode : null
        });

        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Error ?? "We could not create your account. Please try again.");
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            return View(model);
        }

        FlashSuccess("Your account has been created. You can now log in.");
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Ends the session and sends the user back to the login page.</summary>
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        FlashSuccess("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
