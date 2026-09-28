namespace OfficeBite.Shared.DTOs;

public class MenuItemResponseDto
{
    public int MenuItemId { get; set; }

    public string FoodName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; }
}