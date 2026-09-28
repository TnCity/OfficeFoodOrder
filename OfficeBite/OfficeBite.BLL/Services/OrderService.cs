using Microsoft.EntityFrameworkCore;
using OfficeBite.DAL.Data;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;

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

        // Get today's menu
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

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
            CreatedAt = DateTime.UtcNow,
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
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

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
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

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
    // ADMIN - UPDATE ORDER STATUS
    // ==========================================

    public async Task<bool> UpdateStatusAsync(
        int orderId,
        string status)
    {
        var allowedStatuses = new[]
        {
            "Pending",
            "Confirmed",
            "Delivered",
            "Completed",
            "Cancelled"
        };

        if (!allowedStatuses.Contains(status))
        {
            throw new ArgumentException(
                "Invalid order status.");
        }

        var order = await _context.Orders
            .FirstOrDefaultAsync(x =>
                x.OrderId == orderId);

        if (order == null)
            return false;

        order.Status = status;

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