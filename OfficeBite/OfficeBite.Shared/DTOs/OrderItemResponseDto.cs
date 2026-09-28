namespace OfficeBite.Shared.DTOs;

public class OrderItemResponseDto
{
    public int OrderItemId { get; set; }

    public int MenuItemId { get; set; }

    public string FoodName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}