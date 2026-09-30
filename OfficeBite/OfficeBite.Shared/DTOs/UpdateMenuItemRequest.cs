namespace OfficeBite.Shared.DTOs;

public class UpdateMenuItemRequest
{
    public string FoodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}
