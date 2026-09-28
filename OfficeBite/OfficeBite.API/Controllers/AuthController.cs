using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtService _jwtService;

    public AuthController(
        AuthService authService,
        JwtService jwtService)
    {
        _authService = authService;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required.");

        if (request.Password.Length < 6)
            return BadRequest(
                "Password must contain at least 6 characters.");

        var result =
            await _authService.RegisterAsync(request);

        if (!result)
        {
            return Conflict(
                "An account with this email already exists.");
        }

        return Ok(new
        {
            message = "Registration successful."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var user =
            await _authService.ValidateLoginAsync(request);

        if (user == null)
        {
            return Unauthorized(
                "Invalid email or password.");
        }

        var token = _jwtService.GenerateToken(user);

        return Ok(new
        {
            message = "Login successful.",

            token,

            user = new
            {
                user.UserId,
                user.FullName,
                user.Mobile,
                user.Email,
                user.Role
            }
        });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _authService.GetAllEmployeesAsync();
        return Ok(employees);
    }
}