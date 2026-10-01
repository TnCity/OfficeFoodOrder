using OfficeBite.Shared.Helpers;

namespace OfficeBite.DAL.Entities;

public class Menu
{
    public int MenuId { get; set; }

    public DateOnly MenuDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public bool IsOrderingOpen { get; set; }

    public TimeOnly? OrderStartTime { get; set; }

    public TimeOnly? OrderEndTime { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTimeHelper.NowIst;

    public ICollection<MenuItem> MenuItems { get; set; }
        = new List<MenuItem>();

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}