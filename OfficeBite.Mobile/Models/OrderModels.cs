using OfficeBite.Mobile.Helpers;

namespace OfficeBite.Mobile.Models;

public class OrderDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int MenuId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? SpecialInstructions { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public string TotalDisplay => $"Rs.{TotalAmount:0}";
    public string StatusEmoji => Status switch
    {
        "Pending"   => "⏳ Pending",
        "Confirmed" => "👨‍🍳 Order is Under process.",
        "Delivered" => "✅ Delivered",
        "Completed" => "✅ Delivered",
        "Cancelled" => "❌ Cancelled",
        _           => Status
    };
    public Color StatusColor => Status switch
    {
        "Pending"   => Color.FromArgb("#F59E0B"),
        "Confirmed" => Color.FromArgb("#2563EB"),
        "Delivered" => Color.FromArgb("#059669"),
        "Completed" => Color.FromArgb("#059669"),
        "Cancelled" => Color.FromArgb("#EF4444"),
        _           => Color.FromArgb("#6B7280")
    };

    public bool CanModify => Status == "Pending";

    public DateTime? CreatedAtDate => DateTimeHelper.ParseToIst(CreatedAt);

    public string CreatedAtFormatted => DateTimeHelper.FormatIstDateTime(CreatedAt);

    public string TimeFormatted => DateTimeHelper.FormatIstTime(CreatedAt);

    public bool IsToday
    {
        get
        {
            if (CreatedAtDate.HasValue)
            {
                return CreatedAtDate.Value.Date == DateTimeHelper.NowIst.Date;
            }
            return false;
        }
    }

    public string StatusNote => Status switch
    {
        "Pending"   => "⏳ Order placed! Waiting for Admin to accept. You can modify your order anytime before it is accepted.",
        "Confirmed" => "👨‍🍳 Order is Under process. Food is being prepared and will be delivered soon.",
        "Delivered" => "🎉 Order delivered to you! Enjoy your lunch!",
        "Completed" => "🎉 Order delivered to you! Enjoy your lunch!",
        "Cancelled" => "❌ This order was cancelled by Admin. You may place a new order below.",
        _           => "Order status updated."
    };
}

public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int MenuItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string Display => $"{FoodName} x {Quantity}";
    public string PriceDisplay => $"Rs.{TotalPrice:0}";
}

public class CreateOrderRequest
{
    public int MenuId { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public int MenuItemId { get; set; }
    public string? FoodName { get; set; }
    public decimal? UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public class TodaySummaryDto
{
    public string Date { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public List<FoodQuantitySummaryDto> FoodSummary { get; set; } = new();
    public string TotalDisplay => $"Rs.{TotalAmount:0}";
}

public class FoodQuantitySummaryDto
{
    public int MenuItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
}

public class EmployeeLookupDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(Mobile)
        ? $"{FullName} ({Email})"
        : $"{FullName} ({Mobile})";
}

public class AdminCreateOrderRequest
{
    public int UserId { get; set; }
    public string? EmployeeName { get; set; }
    public int MenuId { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
