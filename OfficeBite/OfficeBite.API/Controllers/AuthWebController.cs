using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

[Route("web/auth")]
public class AuthWebController : Controller
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;

    public AuthWebController(AuthService authService, JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    // GET: /web/auth/login
    [HttpGet("login")]
    public IActionResult Login()
    {
        var role = Request.Cookies["UserRole"];
        var name = Request.Cookies["UserName"];
        var userIdStr = Request.Cookies["UserId"];
        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(userIdStr))
        {
            return RedirectToAction("Index", "MenuWeb");
        }

        return View(new LoginRequest());
    }

    // POST: /web/auth/login
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request, [FromForm] bool rememberMe = true)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ViewBag.ErrorMessage = "Email and Password are required.";
            return View(request);
        }

        var user = await _authService.ValidateLoginAsync(request);
        if (user == null)
        {
            ViewBag.ErrorMessage = "Invalid email or password.";
            return View(request);
        }

        SignInUserCookies(user, rememberMe);

        TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";
        return RedirectToAction("Index", "MenuWeb");
    }

    // POST: /web/auth/quick-login/{role}
    [HttpPost("quick-login/{role}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickLogin(string role)
    {
        await _authService.EnsureAdminUsersAsync();

        var user = await _authService.GetFirstUserByRoleAsync(role);
        if (user == null)
        {
            var employees = await _authService.GetAllEmployeesAsync();
            var firstEmp = employees.FirstOrDefault(u => role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? u.Role == "Admin" : u.Role != "Admin") 
                           ?? employees.FirstOrDefault();
            if (firstEmp != null)
            {
                user = await _authService.GetUserByIdAsync(firstEmp.UserId);
            }
        }

        if (user == null)
        {
            TempData["ErrorMessage"] = $"No user found for role '{role}'. Please register first.";
            return RedirectToAction(nameof(Login));
        }

        SignInUserCookies(user, true);

        TempData["SuccessMessage"] = $"Signed in as {user.FullName} ({user.Role})!";
        return RedirectToAction("Index", "MenuWeb");
    }

    private void SignInUserCookies(User user, bool persistent = true)
    {
        var token = _jwtService.GenerateToken(user);
        var expireDate = persistent ? DateTimeOffset.UtcNow.AddDays(30) : (DateTimeOffset?)null;

        var cookieOptions = new CookieOptions 
        { 
            Expires = expireDate,
            IsEssential = true, 
            SameSite = SameSiteMode.Lax 
        };
        var tokenOptions = new CookieOptions 
        { 
            HttpOnly = true, 
            Expires = expireDate,
            IsEssential = true, 
            SameSite = SameSiteMode.Lax 
        };

        Response.Cookies.Append("AuthToken", token, tokenOptions);
        Response.Cookies.Append("UserId", user.UserId.ToString(), cookieOptions);
        Response.Cookies.Append("UserName", user.FullName, cookieOptions);
        Response.Cookies.Append("UserRole", user.Role, cookieOptions);
    }


    //-----------------------------------------------------------------------------------------------------------

    // GET: /web/auth/register
    [HttpGet("register")]
    public IActionResult Register()
    {
        return View(new RegisterRequest());
    }

    // POST: /web/auth/register
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ViewBag.ErrorMessage = "All fields are required.";
            return View(request);
        }

        if (request.Password.Length < 6)
        {
            ViewBag.ErrorMessage = "Password must be at least 6 characters long.";
            return View(request);
        }

        var result = await _authService.RegisterAsync(request);
        if (!result)
        {
            ViewBag.ErrorMessage = "An account with this email already exists.";
            return View(request);
        }

        TempData["SuccessMessage"] = "Account registered successfully! Please sign in.";
        return RedirectToAction(nameof(Login));
    }




    //-----------------------------------------------------------------------------------------------------------

    // GET: /web/auth/employees
    [HttpGet("employees")]
    public async Task<IActionResult> Employees()
    {
        var role = Request.Cookies["UserRole"];
        var name = Request.Cookies["UserName"];
        bool isAdmin = !string.IsNullOrEmpty(name) && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can manage employees.";
            return RedirectToAction(nameof(Login));
        }

        var employees = await _authService.GetAllEmployeesAsync();
        return View(employees);
    }

    // POST: /web/auth/toggle-role/{userId}
    [HttpPost("toggle-role/{userId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRole(int userId)
    {
        var role = Request.Cookies["UserRole"];
        var name = Request.Cookies["UserName"];
        bool isAdmin = !string.IsNullOrEmpty(name) && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can change employee roles.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _authService.ToggleUserRoleAsync(userId);
        if (result)
        {
            TempData["SuccessMessage"] = $"User role updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update user role.";
        }
        return RedirectToAction(nameof(Employees));
    }

    //-----------------------------------------------------------------------------------------------------------

    // GET: /web/auth/logout
    [HttpGet("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        Response.Cookies.Delete("UserId");
        Response.Cookies.Delete("UserName");
        Response.Cookies.Delete("UserRole");

        TempData["SuccessMessage"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }
}
