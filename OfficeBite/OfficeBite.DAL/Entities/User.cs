using OfficeBite.Shared.Helpers;

namespace OfficeBite.DAL.Entities;

public class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Mobile { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Employee";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTimeHelper.NowIst;

    public ICollection<Order> Orders { get; set; }
        = new List<Order>();
}