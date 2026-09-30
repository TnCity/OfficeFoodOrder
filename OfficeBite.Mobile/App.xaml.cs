using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Services;
using OfficeBite.Mobile.Views;

namespace OfficeBite.Mobile;

public partial class App : Application
{
    private readonly ApiService _apiService;
    private readonly LoginPage _loginPage;

    public App(ApiService apiService, LoginPage loginPage)
    {
        InitializeComponent();
        _apiService = apiService;
        _loginPage  = loginPage;

        MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#4F46E5"),
            BarTextColor       = Colors.White
        };

        CheckAutoLogin();
    }

    private async void CheckAutoLogin()
    {
        try
        {
            var isLoggedIn = await AuthHelper.IsLoggedInAsync();
            if (isLoggedIn)
            {
                var role = await AuthHelper.GetRoleAsync();
                ContentPage targetPage = role == "Admin"
                    ? new AdminDashboardPage(_apiService)
                    : new EmployeeDashboardPage(_apiService);

                var barColor = role == "Admin"
                    ? Color.FromArgb("#7C3AED")
                    : Color.FromArgb("#4F46E5");

                MainPage = new NavigationPage(targetPage)
                {
                    BarBackgroundColor = barColor,
                    BarTextColor       = Colors.White
                };
            }
        }
        catch
        {
            // Fallback to login page on any exception
        }
    }
}