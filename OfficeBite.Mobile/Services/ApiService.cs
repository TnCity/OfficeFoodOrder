using System.Net.Http.Headers;
using System.Net.Http.Json;
using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;

namespace OfficeBite.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private async Task AuthorizeAsync()
    {
        var token = await AuthHelper.GetTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrEmpty(token)
                ? null
                : new AuthenticationHeaderValue("Bearer", token);
    }

    // AUTH
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);
        if (!response.IsSuccessStatusCode) return null;
        var raw = await response.Content.ReadFromJsonAsync<LoginRawResponse>();
        if (raw is null) return null;
        return new LoginResponse
        {
            Token    = raw.Token,
            UserId   = raw.User.UserId,
            FullName = raw.User.FullName,
            Email    = raw.User.Email,
            Role     = raw.User.Role
        };
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/Auth/register", request);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    // MENU
    public async Task<MenuDto?> GetTodayMenuAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Menu/today");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MenuDto>();
    }

    public async Task<MenuDto?> GetAdminTodayMenuAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Menu/admin/today");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<MenuDto>();
    }

    public async Task<(bool Success, string Message)> CreateMenuAsync(CreateMenuRequest request)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsJsonAsync("api/Menu", request);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    public async Task<(bool Success, string Message)> AddMenuItemsAsync(int menuId, List<CreateMenuItemRequest> items)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsJsonAsync($"api/Menu/{menuId}/items", items);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    public async Task<(bool Success, string Message)> PublishMenuAsync(int menuId)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsync($"api/Menu/{menuId}/publish", null);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    public async Task<(bool Success, string Message)> CloseOrderingAsync(int menuId)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsync($"api/Menu/{menuId}/close", null);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    // ORDERS
    public async Task<(bool Success, string Message)> PlaceOrderAsync(CreateOrderRequest request)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsJsonAsync("api/Order", request);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Order/my-orders");
        if (!response.IsSuccessStatusCode) return new List<OrderDto>();
        return await response.Content.ReadFromJsonAsync<List<OrderDto>>() ?? new List<OrderDto>();
    }

    public async Task<List<OrderDto>> GetTodayOrdersAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Order/admin/today");
        if (!response.IsSuccessStatusCode) return new List<OrderDto>();
        return await response.Content.ReadFromJsonAsync<List<OrderDto>>() ?? new List<OrderDto>();
    }

    public async Task<TodaySummaryDto?> GetTodaySummaryAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Order/admin/today-summary");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TodaySummaryDto>();
    }

    public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string status)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PutAsync($"api/Order/admin/{orderId}/status?status={status}", null);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    public async Task<List<EmployeeLookupDto>> GetEmployeesAsync()
    {
        await AuthorizeAsync();
        var response = await _httpClient.GetAsync("api/Auth/employees");
        if (!response.IsSuccessStatusCode) return new List<EmployeeLookupDto>();
        return await response.Content.ReadFromJsonAsync<List<EmployeeLookupDto>>() ?? new List<EmployeeLookupDto>();
    }

    public async Task<(bool Success, string Message)> PlaceAdminManualOrderAsync(AdminCreateOrderRequest request)
    {
        await AuthorizeAsync();
        var response = await _httpClient.PostAsJsonAsync("api/Order/admin/manual", request);
        var msg = await SafeReadMessageAsync(response);
        return (response.IsSuccessStatusCode, msg);
    }

    private static async Task<string> SafeReadMessageAsync(HttpResponseMessage response)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return response.ReasonPhrase ?? (response.IsSuccessStatusCode ? "Success" : "Error");

            try
            {
                var raw = System.Text.Json.JsonSerializer.Deserialize<MessageWrapper>(
                    content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!string.IsNullOrWhiteSpace(raw?.Message))
                    return raw.Message;
            }
            catch { }

            return content.Trim('"');
        }
        catch { return response.IsSuccessStatusCode ? "Success" : "Error"; }
    }

    private record MessageWrapper(string? Message);
    private record LoginRawResponse(string Token, UserInfo User);
    private record UserInfo(int UserId, string FullName, string Mobile, string Email, string Role);
}