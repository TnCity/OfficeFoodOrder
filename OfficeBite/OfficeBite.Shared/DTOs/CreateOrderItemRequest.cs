namespace OfficeBite.Shared.DTOs;

public class CreateOrderItemRequest
{
    public int MenuItemId { get; set; }

    public string? FoodName { get; set; }

    public decimal? UnitPrice { get; set; }

    public int Quantity { get; set; } = 1;
}