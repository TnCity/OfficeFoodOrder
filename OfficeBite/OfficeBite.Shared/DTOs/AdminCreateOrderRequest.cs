namespace OfficeBite.Shared.DTOs;

public class AdminCreateOrderRequest
{
    public int? UserId { get; set; }
    public string? EmployeeName { get; set; }
    public int? MenuId { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

