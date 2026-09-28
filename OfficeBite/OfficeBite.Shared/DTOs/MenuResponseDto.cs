namespace OfficeBite.Shared.DTOs;

public class MenuResponseDto
{
    public int MenuId { get; set; }

    public DateOnly MenuDate { get; set; }

    public string Title { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public bool IsOrderingOpen { get; set; }

    public TimeOnly? OrderStartTime { get; set; }

    public TimeOnly? OrderEndTime { get; set; }

    public List<MenuItemResponseDto> Items { get; set; }
        = new();
}