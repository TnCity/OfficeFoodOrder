using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly ApiService _apiService;

    public LoginPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private void QuickAdmin_Clicked(object sender, EventArgs e)
    {
        EmailEntry.Text = "sandeep@officebite.com";
        PasswordEntry.Text = "Admin@123";
        LoginButton_Clicked(sender, e);
    }

    private void QuickEmployee_Clicked(object sender, EventArgs e)
    {
        EmailEntry.Text = "rahul@officebite.com";
        PasswordEntry.Text = "User@123";
        LoginButton_Clicked(sender, e);
    }

    private async void RegisterButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterEmployeePage(_apiService));
    }

    private async void LoginButton_Clicked(object sender, EventArgs e)
    {
        ErrorBorder.IsVisible = false;
        var email    = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter both email and password.");
            return;
        }

        try
        {
            LoginButton.IsEnabled      = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var result = await _apiService.LoginAsync(new LoginRequest { Email = email, Password = password });

            if (result == null)
            {
                ShowError("Invalid email or password. Please check your credentials.");
                return;
            }

            await AuthHelper.SaveLoginAsync(result);

            ContentPage nextPage = result.Role == "Admin"
                ? new AdminDashboardPage(_apiService)
                : new EmployeeDashboardPage(_apiService);

            var navColor = result.Role == "Admin"
                ? Color.FromArgb("#7C3AED")
                : Color.FromArgb("#4F46E5");

            Application.Current!.MainPage = new NavigationPage(nextPage)
            {
                BarBackgroundColor = navColor,
                BarTextColor       = Colors.White
            };
        }
        catch (Exception ex)
        {
            ShowError($"Connection error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled      = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBorder.IsVisible = true;
    }
}