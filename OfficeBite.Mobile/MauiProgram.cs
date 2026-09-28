using Microsoft.Extensions.Logging;
using OfficeBite.Mobile.Services;
using OfficeBite.Mobile.Views;

namespace OfficeBite.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf",  "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // 📡 Local Network IP — phone and PC must be on the same WiFi
            // PC IP: 192.168.31.74  |  API Port: 5052
            builder.Services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri("http://192.168.31.74:5052/"),
                Timeout     = TimeSpan.FromSeconds(30)
            });

            builder.Services.AddSingleton<ApiService>();

            // Pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<EmployeeDashboardPage>();
            builder.Services.AddTransient<TodayMenuPage>();
            builder.Services.AddTransient<ConfirmOrderPage>();
            builder.Services.AddTransient<OrderHistoryPage>();
            builder.Services.AddTransient<AdminDashboardPage>();
            builder.Services.AddTransient<CreateMenuPage>();
            builder.Services.AddTransient<TodayOrdersPage>();
            builder.Services.AddTransient<RegisterEmployeePage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}