namespace OfficeBite.Shared.DTOs;

public class CreateMenuRequest
{
    public string Title { get; set; } = string.Empty;

    public TimeOnly? OrderStartTime { get; set; }

    public TimeOnly? OrderEndTime { get; set; }

    public List<CreateMenuItemRequest> Items { get; set; }
        = new();
}