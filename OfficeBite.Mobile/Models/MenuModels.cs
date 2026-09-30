namespace OfficeBite.Mobile.Models;

public class MenuDto
{
    public int MenuId { get; set; }
    public string MenuDate { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsOrderingOpen { get; set; }
    public string? OrderStartTime { get; set; }
    public string? OrderEndTime { get; set; }
    public List<MenuItemDto> Items { get; set; } = new();
}

public class MenuItemDto
{
    public int MenuItemId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public int Quantity { get; set; } = 0;
    public decimal ItemTotal => Price * Quantity;
    public string PriceDisplay => $"Rs.{Price:0}";
}

public class CreateMenuRequest
{
    public string Title { get; set; } = string.Empty;
    public string? OrderStartTime { get; set; }
    public string? OrderEndTime { get; set; }
    public List<CreateMenuItemRequest> Items { get; set; } = new();
}

public class CreateMenuItemRequest
{
    public string FoodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
}

public class UpdateMenuItemRequest
{
    public string FoodName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
}