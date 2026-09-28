using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class OrderHistoryPage : ContentPage
{
    private readonly ApiService _apiService;

    public OrderHistoryPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopAsync();
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOrdersAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadOrdersAsync();
        RefreshView.IsRefreshing = false;
    }

    private async Task LoadOrdersAsync()
    {
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        OrdersCollection.IsVisible = false;
        NoOrdersBorder.IsVisible   = false;

        try
        {
            var orders = await _apiService.GetMyOrdersAsync();
            if (orders.Count == 0)
            {
                NoOrdersBorder.IsVisible = true;
            }
            else
            {
                OrdersCollection.ItemsSource = orders;
                OrdersCollection.IsVisible   = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load orders: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}