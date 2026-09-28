namespace OfficeBite.DAL.Entities;

public class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int MenuId { get; set; }

    public decimal TotalAmount { get; set; }

    public string? SpecialInstructions { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    // Navigation properties

    public User User { get; set; } = null!;

    public Menu Menu { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
}