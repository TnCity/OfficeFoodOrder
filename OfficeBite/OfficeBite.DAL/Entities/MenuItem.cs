namespace OfficeBite.DAL.Entities;

public class MenuItem
{
    public int MenuItemId { get; set; }

    public int MenuId { get; set; }

    public string FoodName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public Menu Menu { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}