using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;
using OfficeBite.Shared.Helpers;

namespace OfficeBite.API.Controllers;

public class MenuWebIndexViewModel
{
    public MenuResponseDto? Menu { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsLoggedIn { get; set; }
    public int CurrentUserId { get; set; }
    public string CurrentUserName { get; set; } = string.Empty;
    public string CurrentUserRole { get; set; } = string.Empty;
    public OrderResponseDto? ActiveTodayOrder { get; set; }
    public List<EmployeeLookupDto> Employees { get; set; } = new();
}

public class WebPlaceOrderFormModel
{
    public int MenuId { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<WebOrderItemInput> Items { get; set; } = new();
}

public class WebOrderItemInput
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
}

public class WebUpdateMenuItemFormModel
{
    public int MenuItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}

public class WebUpdateMenuDetailsFormModel
{
    public int MenuId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
}

[Route("web/menu")]
public class MenuWebController : Controller
{
    private readonly MenuService _menuService;
    private readonly OrderService _orderService;
    private readonly AuthService _authService;

    public MenuWebController(
        MenuService menuService,
        OrderService orderService,
        AuthService authService)
    {
        _menuService = menuService;
        _orderService = orderService;
        _authService = authService;
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

    // GET: / and /web/menu
    [HttpGet("/")]
    [HttpGet("/index.html")]
    [HttpGet("")]
    [HttpGet("index")]
    public async Task<IActionResult> Index()
    {
        var ctx = GetCurrentUserContext();

        if (!ctx.IsLoggedIn)
        {
            return RedirectToAction("Login", "AuthWeb");
        }

        // Admin gets full menu view (drafts, hidden items); Employee gets live published items
        var menu = ctx.IsAdmin 
            ? await _menuService.GetTodayAdminMenuAsync() 
            : await _menuService.GetTodayMenuAsync();

        OrderResponseDto? activeTodayOrder = null;
        if (ctx.IsLoggedIn && ctx.UserId > 0)
        {
            try
            {
                var myOrders = await _orderService.GetMyOrdersAsync(ctx.UserId);
                activeTodayOrder = myOrders.FirstOrDefault(o => o.Status != "Cancelled" && (menu == null || o.MenuId == menu.MenuId));
            }
            catch
            {
                // Non-critical if user has no orders yet
            }
        }

        var employees = ctx.IsAdmin ? await _authService.GetAllEmployeesAsync() : new List<EmployeeLookupDto>();

        var viewModel = new MenuWebIndexViewModel
        {
            Menu = menu,
            IsAdmin = ctx.IsAdmin,
            IsLoggedIn = ctx.IsLoggedIn,
            CurrentUserId = ctx.UserId,
            CurrentUserName = ctx.UserName,
            CurrentUserRole = ctx.UserRole,
            ActiveTodayOrder = activeTodayOrder,
            Employees = employees
        };

        return View(viewModel);
    }

    // ==========================================
    // EMPLOYEE: PLACE FOOD ORDER
    // ==========================================
    [HttpPost("place-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(WebPlaceOrderFormModel form)
    {
        var ctx = GetCurrentUserContext();

        if (!ctx.IsLoggedIn || ctx.UserId <= 0)
        {
            TempData["ErrorMessage"] = "Please sign in to place an order.";
            return RedirectToAction("Login", "AuthWeb");
        }

        if (!ctx.IsAdmin && DateTimeHelper.IsOrderTimeOver())
        {
            TempData["ErrorMessage"] = "Time is Over, Call Sanjeeb";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Filter only items with quantity > 0
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
                return RedirectToAction(nameof(Index));
            }

            var request = new CreateOrderRequest
            {
                MenuId = form.MenuId,
                SpecialInstructions = form.SpecialInstructions,
                Items = validItems
            };

            var order = await _orderService.CreateOrderAsync(ctx.UserId, request);
            TempData["SuccessMessage"] = $"Order #{order.OrderId} placed successfully! Total: ₹{order.TotalAmount:N2}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // EMPLOYEE: CANCEL PENDING ORDER
    // ==========================================
    [HttpPost("cancel-my-order")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelMyOrder(int orderId)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsLoggedIn || ctx.UserId <= 0)
        {
            TempData["ErrorMessage"] = "Please sign in.";
            return RedirectToAction("Login", "AuthWeb");
        }

        if (!ctx.IsAdmin && DateTimeHelper.IsOrderTimeOver())
        {
            TempData["ErrorMessage"] = "Time is Over, Call Sanjeeb";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _orderService.DeleteOrderAsync(ctx.UserId, orderId);
            TempData["SuccessMessage"] = "Your order has been cancelled successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // ==========================================
    // ADMIN ACTIONS (Restricted to Admins)
    // ==========================================

    [HttpGet("create")]
    public IActionResult Create()
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can create menus.";
            return RedirectToAction(nameof(Index));
        }

        var model = new CreateMenuRequest
        {
            Title = "Daily Lunch Menu",
            Items = new List<CreateMenuItemRequest>
            {
                new() { FoodName = "Chicken Biryani", Price = 150 },
                new() { FoodName = "Veg Thali", Price = 100 }
            }
        };
        return View(model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMenuRequest request)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can create menus.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var created = await _menuService.CreateMenuAsync(request, ctx.UserId > 0 ? ctx.UserId : 1);
            TempData["SuccessMessage"] = $"Menu '{created.Title}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(request);
        }
    }

    [HttpPost("publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int menuId)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var success = await _menuService.PublishMenuAsync(menuId);
            if (success)
                TempData["SuccessMessage"] = "Today's menu has been published to employees!";
            else
                TempData["ErrorMessage"] = "Failed to publish menu.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseOrdering(int menuId)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var success = await _menuService.CloseOrderingAsync(menuId);
            if (success)
                TempData["SuccessMessage"] = "Food ordering for today has been closed.";
            else
                TempData["ErrorMessage"] = "Failed to close ordering.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("reset-standard")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetStandard()
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _menuService.AutoSeedTodayMenuAsync();
            TempData["SuccessMessage"] = "Standard daily menu loaded successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("toggle-item/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleItem(int id)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Employees cannot enable/disable menu items.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var updated = await _menuService.ToggleMenuItemAvailabilityAsync(id);
            TempData["SuccessMessage"] = $"{updated.FoodName} is now {(updated.IsAvailable ? "Available" : "Unavailable")}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("add-item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int menuId, string foodName, string? description, decimal price)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Employees cannot add menu items.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(foodName) || price <= 0)
            {
                TempData["ErrorMessage"] = "Please provide a valid Food Name and Price.";
                return RedirectToAction(nameof(Index));
            }

            var items = new List<CreateMenuItemRequest>
            {
                new() { FoodName = foodName.Trim(), Description = description?.Trim(), Price = price }
            };

            await _menuService.AddMenuItemsAsync(menuId, items);
            TempData["SuccessMessage"] = $"Added '{foodName}' to today's menu!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update-item")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(WebUpdateMenuItemFormModel form)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can edit menu items.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            if (string.IsNullOrWhiteSpace(form.FoodName) || form.Price <= 0)
            {
                TempData["ErrorMessage"] = "Please provide a valid Food Name and Price.";
                return RedirectToAction(nameof(Index));
            }

            var request = new UpdateMenuItemRequest
            {
                FoodName = form.FoodName.Trim(),
                Description = form.Description?.Trim(),
                Price = form.Price,
                IsAvailable = form.IsAvailable
            };

            await _menuService.UpdateMenuItemAsync(form.MenuItemId, request);
            TempData["SuccessMessage"] = $"Updated '{form.FoodName}' successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete-item/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can delete menu items.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var success = await _menuService.DeleteMenuItemAsync(id);
            if (success)
                TempData["SuccessMessage"] = "Food item removed from menu.";
            else
                TempData["ErrorMessage"] = "Failed to remove item.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("update-menu-details")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMenuDetails(WebUpdateMenuDetailsFormModel form)
    {
        var ctx = GetCurrentUserContext();
        if (!ctx.IsAdmin)
        {
            TempData["ErrorMessage"] = "Access denied: Only Admins can edit menu details.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            TimeOnly? parsedStart = null;
            if (TimeOnly.TryParse(form.StartTime, out var st)) parsedStart = st;

            TimeOnly? parsedEnd = null;
            if (TimeOnly.TryParse(form.EndTime, out var et)) parsedEnd = et;

            await _menuService.UpdateMenuDetailsAsync(form.MenuId, form.Title, parsedStart, parsedEnd);
            TempData["SuccessMessage"] = "Menu details updated successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
