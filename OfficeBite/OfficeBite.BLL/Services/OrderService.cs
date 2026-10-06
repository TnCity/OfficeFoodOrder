using Microsoft.EntityFrameworkCore;
using OfficeBite.DAL.Data;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;
using OfficeBite.Shared.Helpers;

namespace OfficeBite.BLL.Services;

public class OrderService
{
    private readonly OfficeBiteDbContext _context;

    public OrderService(OfficeBiteDbContext context)
    {
        _context = context;
    }


    // ==========================================
    // EMPLOYEE - CREATE ORDER
    // ==========================================

    public async Task<OrderResponseDto> CreateOrderAsync(
        int userId,
        CreateOrderRequest request)
    {
        if (request.Items == null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "Please select at least one food item.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        // Get today's menu (IST)
        var today = DateTimeHelper.TodayIst;

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuId == request.MenuId &&
                x.MenuDate == today &&
                x.IsPublished);

        if (menu == null)
        {
            throw new InvalidOperationException(
                "Today's menu is not available.");
        }

        var order = new Order
        {
            UserId = userId,
            MenuId = menu.MenuId,
            Status = "Pending",
            SpecialInstructions = request.SpecialInstructions?.Trim(),
            CreatedAt = DateTimeHelper.NowIst,
            TotalAmount = 0
        };


        decimal totalAmount = 0;


        foreach (var requestItem in request.Items)
        {
            var menuItem = menu.MenuItems
                .FirstOrDefault(x =>
                    x.MenuItemId ==
                    requestItem.MenuItemId);

            if (menuItem == null)
            {
                throw new ArgumentException(
                    $"Food item {requestItem.MenuItemId} " +
                    "does not belong to today's menu.");
            }

            if (!menuItem.IsAvailable)
            {
                throw new InvalidOperationException(
                    $"{menuItem.FoodName} is currently unavailable.");
            }


            var itemTotal =
                menuItem.Price * requestItem.Quantity;


            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.MenuItemId,

                Quantity = requestItem.Quantity,

                UnitPrice = menuItem.Price,

                TotalPrice = itemTotal
            };


            order.OrderItems.Add(orderItem);

            totalAmount += itemTotal;
        }


        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);

        await _context.SaveChangesAsync();


        // Load navigation properties for response
        await _context.Entry(order)
            .Reference(x => x.User)
            .LoadAsync();

        await _context.Entry(order)
            .Collection(x => x.OrderItems)
            .Query()
            .Include(x => x.MenuItem)
            .LoadAsync();


        return MapOrder(order);
    }


    // ==========================================
    // EMPLOYEE - UPDATE/MODIFY PENDING ORDER
    // ==========================================

    public async Task<OrderResponseDto> UpdateOrderAsync(
        int userId,
        int orderId,
        CreateOrderRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            throw new ArgumentException("Please select at least one food item.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        var order = await _context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.UserId == userId);

        if (order == null)
        {
            throw new ArgumentException("Order not found.");
        }

        if (order.Status != "Pending")
        {
            throw new InvalidOperationException("Order is Under process or completed and cannot be modified.");
        }

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.MenuId == order.MenuId);

        if (menu == null)
        {
            throw new InvalidOperationException("Today's menu is not available.");
        }

        // Remove old items
        _context.OrderItems.RemoveRange(order.OrderItems);
        order.OrderItems.Clear();

        decimal totalAmount = 0;
        foreach (var requestItem in request.Items)
        {
            var menuItem = menu.MenuItems.FirstOrDefault(x => x.MenuItemId == requestItem.MenuItemId);
            if (menuItem == null)
            {
                throw new ArgumentException($"Food item does not belong to today's menu.");
            }

            if (!menuItem.IsAvailable)
            {
                throw new InvalidOperationException($"{menuItem.FoodName} is currently unavailable.");
            }

            var itemTotal = menuItem.Price * requestItem.Quantity;
            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.MenuItemId,
                Quantity = requestItem.Quantity,
                UnitPrice = menuItem.Price,
                TotalPrice = itemTotal
            };
            order.OrderItems.Add(orderItem);
            totalAmount += itemTotal;
        }

        order.SpecialInstructions = request.SpecialInstructions?.Trim();
        order.TotalAmount = totalAmount;

        await _context.SaveChangesAsync();

        await _context.Entry(order).Reference(x => x.User).LoadAsync();
        await _context.Entry(order).Collection(x => x.OrderItems).Query().Include(x => x.MenuItem).LoadAsync();

        return MapOrder(order);
    }

    public async Task<bool> DeleteOrderAsync(int userId, int orderId)
    {
        var order = await _context.Orders
            .Include(x => x.OrderItems)
            .FirstOrDefaultAsync(x => x.OrderId == orderId && x.UserId == userId);

        if (order == null)
            throw new ArgumentException("Order not found.");

        if (order.Status != "Pending")
            throw new InvalidOperationException("Order is Under process or completed and cannot be deleted.");

        _context.OrderItems.RemoveRange(order.OrderItems);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return true;
    }


    // ==========================================
    // EMPLOYEE - OWN ORDER HISTORY
    // ==========================================

    public async Task<List<OrderResponseDto>>
        GetMyOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(x => x.UserId == userId)
            .Include(x => x.User)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return orders
            .Select(MapOrder)
            .ToList();
    }


    public async Task<TodayOrderSummaryDto>
    GetTodaySummaryAsync()
    {
        var today = DateTimeHelper.TodayIst;

        var orders = await _context.Orders
            .Include(x => x.Menu)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .Where(x => x.Menu.MenuDate == today)
            .ToListAsync();

        var summary = new TodayOrderSummaryDto
        {
            Date = today,

            TotalOrders = orders.Count,

            PendingOrders = orders.Count(
                x => x.Status == "Pending"),

            ConfirmedOrders = orders.Count(
                x => x.Status == "Confirmed"),

            DeliveredOrders = orders.Count(
                x => x.Status == "Delivered" || x.Status == "Completed" || x.Status == "Ready"),

            CancelledOrders = orders.Count(
                x => x.Status == "Cancelled"),

            TotalAmount = orders
                .Where(x => x.Status != "Cancelled")
                .Sum(x => x.TotalAmount)
        };

        summary.FoodSummary = orders
            .Where(x => x.Status != "Cancelled")
            .SelectMany(x => x.OrderItems)
            .GroupBy(x => new
            {
                x.MenuItemId,
                FoodName = x.MenuItem.FoodName
            })
            .Select(g => new FoodQuantitySummaryDto
            {
                MenuItemId = g.Key.MenuItemId,

                FoodName = g.Key.FoodName,

                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.FoodName)
            .ToList();

        return summary;
    }

    // ==========================================
    // EMPLOYEE - GET SINGLE ORDER
    // ==========================================

    public async Task<OrderResponseDto?> GetMyOrderAsync(
        int userId,
        int orderId)
    {
        var order = await _context.Orders
            .Include(x => x.User)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .FirstOrDefaultAsync(x =>
                x.OrderId == orderId &&
                x.UserId == userId);

        if (order == null)
            return null;

        return MapOrder(order);
    }


    // ==========================================
    // ADMIN - ALL ORDERS
    // ==========================================

    public async Task<List<OrderResponseDto>>
        GetAllOrdersAsync()
    {
        var orders = await _context.Orders
            .Include(x => x.User)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return orders
            .Select(MapOrder)
            .ToList();
    }


    // ==========================================
    // ADMIN - TODAY'S ORDERS
    // ==========================================

    public async Task<List<OrderResponseDto>>
        GetTodayOrdersAsync()
    {
        var today = DateTimeHelper.TodayIst;

        var orders = await _context.Orders
            .Include(x => x.User)
            .Include(x => x.OrderItems)
                .ThenInclude(x => x.MenuItem)
            .Where(x =>
                x.Menu.MenuDate == today)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        return orders
            .Select(MapOrder)
            .ToList();
    }


    // ==========================================
    // ADMIN - CREATE MANUAL ORDER FOR EMPLOYEE
    // ==========================================

    public async Task<OrderResponseDto> CreateAdminManualOrderAsync(
        AdminCreateOrderRequest request)
    {
        if (request == null)
            throw new ArgumentException("Order request cannot be null.");

        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Please add at least one food item.");

        if (request.Items.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Item quantities must be greater than zero.");

        // 1. Resolve Employee (User)
        User? user = null;
        if (request.UserId.HasValue && request.UserId.Value > 0)
        {
            user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId.Value);
        }

        if (user == null && !string.IsNullOrWhiteSpace(request.EmployeeName))
        {
            var cleanName = request.EmployeeName.Trim();
            // Case-insensitive lookup
            user = await _context.Users
                .FirstOrDefaultAsync(u => u.FullName.ToLower() == cleanName.ToLower());

            if (user == null)
            {
                var slug = System.Text.RegularExpressions.Regex.Replace(cleanName.ToLower(), @"[^a-z0-9]", ".");
                slug = slug.Trim('.');
                if (string.IsNullOrEmpty(slug)) slug = "employee";

                var autoEmail = $"{slug}@officebite.local";
                if (await _context.Users.AnyAsync(u => u.Email == autoEmail))
                {
                    autoEmail = $"{slug}.{Random.Shared.Next(100, 999)}@officebite.local";
                }

                user = new User
                {
                    FullName = cleanName,
                    Email = autoEmail,
                    Mobile = "9999999999",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                    Role = "Employee",
                    IsActive = true,
                    CreatedAt = DateTimeHelper.NowIst
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
        }

        if (user == null)
        {
            throw new ArgumentException("Please enter the employee's name.");
        }

        // 2. Resolve Menu
        var today = DateTimeHelper.TodayIst;
        Menu? menu = null;

        if (request.MenuId.HasValue && request.MenuId.Value > 0)
        {
            menu = await _context.Menus
                .Include(m => m.MenuItems)
                .FirstOrDefaultAsync(m => m.MenuId == request.MenuId.Value);
        }

        if (menu == null)
        {
            menu = await _context.Menus
                .Include(m => m.MenuItems)
                .FirstOrDefaultAsync(m => m.MenuDate == today);
        }

        if (menu == null)
        {
            menu = new Menu
            {
                MenuDate = today,
                Title = $"Lunch Menu - {today:dd MMM yyyy}",
                IsPublished = true,
                IsOrderingOpen = true,
                CreatedAt = DateTimeHelper.NowIst
            };
            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
        }

        var order = new Order
        {
            UserId = user.UserId,
            MenuId = menu.MenuId,
            Status = "Confirmed",
            SpecialInstructions = request.SpecialInstructions?.Trim(),
            CreatedAt = DateTimeHelper.NowIst,
            TotalAmount = 0
        };

        decimal totalAmount = 0;

        foreach (var reqItem in request.Items)
        {
            MenuItem? menuItem = null;

            if (reqItem.MenuItemId > 0)
            {
                menuItem = menu.MenuItems.FirstOrDefault(m => m.MenuItemId == reqItem.MenuItemId)
                    ?? await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemId == reqItem.MenuItemId);
            }

            if (menuItem == null)
            {
                var dishName = string.IsNullOrWhiteSpace(reqItem.FoodName) ? "Custom Food Item" : reqItem.FoodName.Trim();

                menuItem = menu.MenuItems
                    .FirstOrDefault(m => m.FoodName.Equals(dishName, StringComparison.OrdinalIgnoreCase));

                if (menuItem == null)
                {
                    decimal defaultPrice = (reqItem.UnitPrice.HasValue && reqItem.UnitPrice.Value >= 0)
                        ? reqItem.UnitPrice.Value
                        : 0;

                    menuItem = new MenuItem
                    {
                        MenuId = menu.MenuId,
                        FoodName = dishName,
                        Price = defaultPrice,
                        IsAvailable = true,
                        Description = "Custom manual order item"
                    };
                    _context.MenuItems.Add(menuItem);
                    await _context.SaveChangesAsync();
                    menu.MenuItems.Add(menuItem);
                }
            }

            decimal unitPrice = (reqItem.UnitPrice.HasValue && reqItem.UnitPrice.Value >= 0)
                ? reqItem.UnitPrice.Value
                : menuItem.Price;

            int qty = reqItem.Quantity > 0 ? reqItem.Quantity : 1;
            decimal itemTotal = unitPrice * qty;

            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.MenuItemId,
                Quantity = qty,
                UnitPrice = unitPrice,
                TotalPrice = itemTotal
            };

            order.OrderItems.Add(orderItem);
            totalAmount += itemTotal;
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _context.Entry(order)
            .Reference(x => x.User)
            .LoadAsync();

        await _context.Entry(order)
            .Collection(x => x.OrderItems)
            .Query()
            .Include(x => x.MenuItem)
            .LoadAsync();

        return MapOrder(order);
    }


    // ==========================================
    // ADMIN - UPDATE ORDER STATUS
    // ==========================================

    public async Task<bool> UpdateStatusAsync(
        int orderId,
        string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Order status cannot be empty.");

        var validStatuses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Pending", "Pending" },
            { "Confirmed", "Confirmed" },
            { "Cooking", "Cooking" },
            { "Ready", "Ready" },
            { "Delivered", "Delivered" },
            { "Completed", "Completed" },
            { "Cancelled", "Cancelled" }
        };

        if (!validStatuses.TryGetValue(status.Trim(), out var canonicalStatus))
        {
            throw new ArgumentException($"Invalid order status '{status}'. Allowed statuses are: {string.Join(", ", validStatuses.Values)}");
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(x => x.OrderId == orderId);

        if (order == null)
            return false;

        order.Status = canonicalStatus;

        await _context.SaveChangesAsync();

        return true;
    }


    // ==========================================
    // MAPPING
    // ==========================================

    private static OrderResponseDto MapOrder(
        Order order)
    {
        return new OrderResponseDto
        {
            OrderId = order.OrderId,

            UserId = order.UserId,

            UserName = order.User?.FullName
                ?? string.Empty,

            MenuId = order.MenuId,

            CreatedAt = order.CreatedAt,

            Status = order.Status,

            SpecialInstructions = order.SpecialInstructions,

            TotalAmount = order.TotalAmount,

            Items = order.OrderItems
                .Select(x => new OrderItemResponseDto
                {
                    OrderItemId = x.OrderItemId,

                    MenuItemId = x.MenuItemId,

                    FoodName =
                        x.MenuItem?.FoodName
                        ?? string.Empty,

                    Quantity = x.Quantity,

                    UnitPrice = x.UnitPrice,

                    TotalPrice = x.TotalPrice
                })
                .ToList()
        };
    }
}