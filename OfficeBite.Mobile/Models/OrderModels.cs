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
        "Pending"   => "Pending",
        "Confirmed" => "Confirmed",
        "Preparing" => "Preparing",
        "Ready"     => "Ready!",
        "Completed" => "Completed",
        "Cancelled" => "Cancelled",
        _           => Status
    };
    public Color StatusColor => Status switch
    {
        "Pending"   => Color.FromArgb("#F59E0B"),
        "Confirmed" => Color.FromArgb("#3B82F6"),
        "Preparing" => Color.FromArgb("#8B5CF6"),
        "Ready"     => Color.FromArgb("#10B981"),
        "Completed" => Color.FromArgb("#6B7280"),
        "Cancelled" => Color.FromArgb("#EF4444"),
        _           => Color.FromArgb("#6B7280")
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
    public int Quantity { get; set; }
}

public class TodaySummaryDto
{
    public string Date { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ConfirmedOrders { get; set; }
    public int PreparingOrders { get; set; }
    public int ReadyOrders { get; set; }
    public int CompletedOrders { get; set; }
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