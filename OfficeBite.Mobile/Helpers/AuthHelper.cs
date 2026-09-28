using OfficeBite.Mobile.Models;

namespace OfficeBite.Mobile.Helpers;

public static class AuthHelper
{
    private const string KeyToken    = "auth_token";
    private const string KeyUserId   = "user_id";
    private const string KeyUserName = "user_name";
    private const string KeyEmail    = "user_email";
    private const string KeyRole     = "user_role";

    public static async Task SaveLoginAsync(LoginResponse response)
    {
        await SecureStorage.SetAsync(KeyToken,    response.Token);
        await SecureStorage.SetAsync(KeyUserId,   response.UserId.ToString());
        await SecureStorage.SetAsync(KeyUserName, response.FullName);
        await SecureStorage.SetAsync(KeyEmail,    response.Email);
        await SecureStorage.SetAsync(KeyRole,     response.Role);
    }

    public static async Task<string?> GetTokenAsync()
        => await SecureStorage.GetAsync(KeyToken);

    public static async Task<int> GetUserIdAsync()
    {
        var raw = await SecureStorage.GetAsync(KeyUserId);
        return int.TryParse(raw, out var id) ? id : 0;
    }

    public static async Task<string> GetUserNameAsync()
        => await SecureStorage.GetAsync(KeyUserName) ?? string.Empty;

    public static async Task<string> GetRoleAsync()
        => await SecureStorage.GetAsync(KeyRole) ?? string.Empty;

    public static async Task<bool> IsAdminAsync()
    {
        var role = await GetRoleAsync();
        return role == "Admin";
    }

    public static async Task<bool> IsLoggedInAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public static void Logout()
    {
        SecureStorage.Remove(KeyToken);
        SecureStorage.Remove(KeyUserId);
        SecureStorage.Remove(KeyUserName);
        SecureStorage.Remove(KeyEmail);
        SecureStorage.Remove(KeyRole);
    }
}