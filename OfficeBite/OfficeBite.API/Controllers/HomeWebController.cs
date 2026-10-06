using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

public class HomeDashboardViewModel
{
    public MenuResponseDto? TodayMenu { get; set; }
    public TodayOrderSummaryDto? TodaySummary { get; set; }
    public List<OrderResponseDto> RecentOrders { get; set; } = new();
    public int TotalEmployees { get; set; }
    public string CurrentUserName { get; set; } = string.Empty;
    public string CurrentUserRole { get; set; } = string.Empty;
}

public class HomeWebController : Controller
{
    private readonly MenuService _menuService;
    private readonly OrderService _orderService;
    private readonly AuthService _authService;

    public HomeWebController(
        MenuService menuService,
        OrderService orderService,
        AuthService authService)
    {
        _menuService = menuService;
        _orderService = orderService;
        _authService = authService;
    }

    // GET: /dashboard and /home
    [HttpGet("/dashboard")]
    [HttpGet("/home")]
    [HttpGet("/web/home")]
    public async Task<IActionResult> Index()
    {
        var role = Request.Cookies["UserRole"];
        var name = Request.Cookies["UserName"];
        bool isAdmin = !string.IsNullOrEmpty(name) && string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        if (!isAdmin)
        {
            return RedirectToAction("Index", "MenuWeb");
        }

        // Ensure sanjeeb/sanjeem and admin candidates have Admin role
        await _authService.EnsureAdminUsersAsync();

        var todayMenu = await _menuService.GetTodayAdminMenuAsync();
        var todaySummary = await _orderService.GetTodaySummaryAsync();
        var allOrders = await _orderService.GetAllOrdersAsync();
        var employees = await _authService.GetAllEmployeesAsync();

        var viewModel = new HomeDashboardViewModel
        {
            TodayMenu = todayMenu,
            TodaySummary = todaySummary,
            RecentOrders = allOrders.Take(5).ToList(),
            TotalEmployees = employees.Count,
            CurrentUserName = name ?? "Admin",
            CurrentUserRole = role ?? "Admin"
        };

        return View(viewModel);
    }
}
