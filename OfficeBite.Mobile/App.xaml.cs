using OfficeBite.Mobile.Services;
using OfficeBite.Mobile.Views;

namespace OfficeBite.Mobile;

public partial class App : Application
{
    public App(LoginPage loginPage)
    {
        InitializeComponent();
        MainPage = new NavigationPage(loginPage)
        {
            BarBackgroundColor = Color.FromArgb("#4F46E5"),
            BarTextColor       = Colors.White
        };
    }
}