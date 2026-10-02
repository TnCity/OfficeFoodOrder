using Android.App;
using Android.Content.PM;
using Android.OS;

namespace OfficeBite.Mobile
{
    [Activity(Label = "CityBite", Icon = "@mipmap/appicon", RoundIcon = "@mipmap/appicon_round", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
    }
}
