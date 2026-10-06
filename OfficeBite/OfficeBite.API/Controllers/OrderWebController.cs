using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

public class EditOrderViewModel
{
    public OrderResponseDto Order { get; set; } = new();
    public MenuResponseDto Menu { get; set; } = new();
    public Dictionary<int, int> CurrentItemQuantities { get; set; } = new();
}

public class WebCreateManualOrderItemInput
{
    public int MenuItemId { get; set; }
    public string? FoodName { get; set; }
    public decimal? UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class WebCreateManualOrderFormModel
{
    public string EmployeeName { get; set; } = string.Empty;
    public int? MenuId { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<WebCreateManualOrderItemInput> Items { get; set; } = new();
}

public class CreateManualOrderViewModel
{
    public MenuResponseDto? Menu { get; set; }
    public bool IsAdmin { get; set; }
}

public record UserWebContext(int UserId, string UserName, string UserRole, bool IsAdmin, bool IsLoggedIn);


[Route("web/orders")]
public class OrderWebController : Controller
{
    private readonly OrderService _orderService;
    private readonly MenuService _menuService;

    public OrderWebController(OrderService orderService, MenuService menuService)
    {
        _orderService = orderService;
        _menuService = menuService;
    }

    private UserWebContext GetCurrentUserContext()
    {
        var role = Request.Cookies["UserRole"] ?? string.Empty;
        var name = Request.Cookies["UserName"] ?? string.Empty;
        int.TryParse(Request.Cookies["UserId"], out var userId);

        bool isLoggedIn = !string.IsNullOrEmpty(name) && userId > 0;
        bool isAdmin = isLoggedIn && role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        return new UserWebContext(isLoggedIn ? userId : 0, name, role, isAdmin, isLoggedIn);
    }

    // GET: /web/orders
    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index([FromQuery] string? filter)
    {
        var ctx = GetCurrentUserContext();

        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in to view orders.";
            return RedirectToAction("Login", "AuthWeb");
        }

        List<OrderResponseDto> orders;

        if (!ctx.IsAdmin)
        {
            // Employee only sees their own orders
            orders = await _orderService.GetMyOrdersAsync(ctx.UserId);
            ViewBag.IsEmployeeView = true;
        }
        else
        {
            // Admin sees all orders or today's orders
            orders = filter switch
            {
                "today" => await _orderService.GetTodayOrdersAsync(),
                _ => await _orderService.GetAllOrdersAsync()
            };
            ViewBag.IsEmployeeView = false;
        }

        ViewBag.CurrentFilter = filter ?? "all";
        ViewBag.IsAdmin = ctx.IsAdmin;
        return View(orders);
    }

    // GET: /web/orders/today-summary
    [HttpGet("today-summary")]
    public async Task<IActionResult> TodaySummary()
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in to view the kitchen summary.";
            return RedirectToAction("Login", "AuthWeb");
        }

        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can view the kitchen summary.";
            return RedirectToAction(nameof(Index));
        }

        var summary = await _orderService.GetTodaySummaryAsync();
        return View(summary);
    }

    // GET: /web/orders/details/{id}
    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var ctx = GetCurrentUserContext();

        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in to view order details.";
            return RedirectToAction("Login", "AuthWeb");
        }

        var allOrders = await _orderService.GetAllOrdersAsync();
        var order = allOrders.FirstOrDefault(o => o.OrderId == id);

        if (order == null)
        {
            TempData["ErrorMessage"] = $"Order #{id} was not found.";
            return RedirectToAction(nameof(Index));
        }

        // Employee can only view their own order
        if (!ctx.IsAdmin && order.UserId != ctx.UserId)
        {
            TempData["ErrorMessage"] = "Access denied: You can only view your own order details.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.IsAdmin = ctx.IsAdmin;
        ViewBag.CurrentUserId = ctx.UserId;
        return View(order);
    }

    // GET: /web/orders/edit/{id}
    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        var allOrders = await _orderService.GetAllOrdersAsync();
        var order = allOrders.FirstOrDefault(o => o.OrderId == id);

        if (order == null)
        {
            TempData["ErrorMessage"] = $"Order #{id} was not found.";
            return RedirectToAction(nameof(Index));
        }

        if (!ctx.IsAdmin && order.UserId != ctx.UserId)
        {
            TempData["ErrorMessage"] = "Access denied: You can only edit your own order.";
            return RedirectToAction(nameof(Index));
        }

        if (order.Status != "Pending")
        {
            TempData["ErrorMessage"] = $"Order #{id} is '{order.Status}' and cannot be modified.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var menu = await _menuService.GetTodayMenuAsync() ?? await _menuService.GetTodayAdminMenuAsync();
        if (menu == null)
        {
            TempData["ErrorMessage"] = "Today's menu is not available.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var currentQuantities = order.Items.ToDictionary(i => i.MenuItemId, i => i.Quantity);

        var viewModel = new EditOrderViewModel
        {
            Order = order,
            Menu = menu,
            CurrentItemQuantities = currentQuantities
        };

        ViewBag.IsAdmin = ctx.IsAdmin;
        return View(viewModel);
    }

    // POST: /web/orders/edit/{id}
    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, WebPlaceOrderFormModel form)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        try
        {
            var validItems = form.Items
                .Where(x => x.Quantity > 0)
                .Select(x => new CreateOrderItemRequest
                {
                    MenuItemId = x.MenuItemId,
                    Quantity = x.Quantity
                })
                .ToList();

            if (!validItems.Any())
            {
                TempData["ErrorMessage"] = "Please select at least 1 food item portion.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var request = new CreateOrderRequest
            {
                MenuId = form.MenuId,
                SpecialInstructions = form.SpecialInstructions,
                Items = validItems
            };

            var updated = await _orderService.UpdateOrderAsync(ctx.UserId, id, request);
            TempData["SuccessMessage"] = $"Order #{id} has been updated successfully! New Total: ₹{updated.TotalAmount:N2}";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Edit), new { id });
        }
    }

    // POST: /web/orders/cancel/{id}
    [HttpPost("cancel/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        try
        {
            await _orderService.DeleteOrderAsync(ctx.UserId, id);
            TempData["SuccessMessage"] = $"Order #{id} has been cancelled successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // POST: /web/orders/update-status (ADMIN ONLY)
    [HttpPost("update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string status, string? returnUrl)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can change order status.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var success = await _orderService.UpdateStatusAsync(orderId, status);
            if (success)
            {
                TempData["SuccessMessage"] = $"Order #{orderId} status updated to '{status}'.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Failed to update order #{orderId}.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    // GET: /web/orders/create-manual (ADMIN ONLY)
    [HttpGet("create-manual")]
    public async Task<IActionResult> CreateManual()
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can take manual orders for employees.";
            return RedirectToAction(nameof(Index));
        }

        var menu = await _menuService.GetTodayMenuAsync() ?? await _menuService.GetTodayAdminMenuAsync();

        var viewModel = new CreateManualOrderViewModel
        {
            Menu = menu,
            IsAdmin = ctx.IsAdmin
        };

        ViewBag.IsAdmin = ctx.IsAdmin;
        return View(viewModel);
    }

    // POST: /web/orders/create-manual (ADMIN ONLY)
    [HttpPost("create-manual")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManual(WebCreateManualOrderFormModel form)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can take manual orders.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(form.EmployeeName))
        {
            TempData["ErrorMessage"] = "Please enter the Employee Name.";
            return RedirectToAction(nameof(CreateManual));
        }

        var validItems = (form.Items ?? new List<WebCreateManualOrderItemInput>())
            .Where(x => x.Quantity > 0 && (x.MenuItemId > 0 || !string.IsNullOrWhiteSpace(x.FoodName)))
            .Select(x => new CreateOrderItemRequest
            {
                MenuItemId = x.MenuItemId,
                FoodName = x.FoodName?.Trim(),
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity
            })
            .ToList();

        if (!validItems.Any())
        {
            TempData["ErrorMessage"] = "Please select or add at least 1 food dish portion.";
            return RedirectToAction(nameof(CreateManual));
        }

        try
        {
            var adminRequest = new AdminCreateOrderRequest
            {
                EmployeeName = form.EmployeeName.Trim(),
                MenuId = form.MenuId,
                SpecialInstructions = form.SpecialInstructions?.Trim(),
                Items = validItems
            };

            var order = await _orderService.CreateAdminManualOrderAsync(adminRequest);
            TempData["SuccessMessage"] = $"Manual order #{order.OrderId} created successfully for {order.UserName}! Total: ₹{order.TotalAmount:N2}";
            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(CreateManual));
        }
    }
}

