using Microsoft.EntityFrameworkCore;
using OfficeBite.DAL.Data;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.BLL.Services;

public class AuthService
{
    private readonly OfficeBiteDbContext _context;

    public AuthService(OfficeBiteDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RegisterAsync(
        RegisterRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == email);

        if (existingUser != null)
            return false;

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Mobile = request.Mobile.Trim(),
            Email = email,

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password),

            Role = "Employee",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<User?> ValidateLoginAsync(
        LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();

        var user = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Email == email);

        if (user == null)
            return null;

        if (!user.IsActive)
            return null;

        var passwordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!passwordValid)
            return null;

        return user;
    }

    public async Task<List<EmployeeLookupDto>> GetAllEmployeesAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new EmployeeLookupDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Mobile = u.Mobile,
                Role = u.Role
            })
            .ToListAsync();
    }
}