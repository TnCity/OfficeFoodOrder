namespace OfficeBite.Shared.DTOs;

public class CreateOrderRequest
{
    public int MenuId { get; set; }

    public string? SpecialInstructions { get; set; }

    public List<CreateOrderItemRequest> Items { get; set; }
        = new();
}