namespace OfficeBite.Shared.DTOs;

public class TodayOrderSummaryDto
{
    public DateOnly Date { get; set; }

    public int TotalOrders { get; set; }

    public int PendingOrders { get; set; }

    public int ConfirmedOrders { get; set; }

    public int DeliveredOrders { get; set; }

    public int CancelledOrders { get; set; }

    public decimal TotalAmount { get; set; }

    public List<FoodQuantitySummaryDto> FoodSummary { get; set; } = new();
}