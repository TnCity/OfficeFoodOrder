namespace OfficeBite.Shared.DTOs;

public class OrderResponseDto
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public int MenuId { get; set; }

    public object CreatedAt { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? SpecialInstructions { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemResponseDto> Items { get; set; }
        = new();
}