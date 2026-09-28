namespace OfficeBite.Shared.DTOs;

public class FoodQuantitySummaryDto
{
    public int MenuItemId { get; set; }

    public string FoodName { get; set; } = string.Empty;

    public int TotalQuantity { get; set; }
}