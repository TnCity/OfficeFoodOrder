namespace OfficeBite.Shared.DTOs;

public class CreateMenuItemRequest
{
    public string FoodName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }
}