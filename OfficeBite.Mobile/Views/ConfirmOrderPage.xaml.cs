using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class ConfirmOrderPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly MenuDto _menu;
    private readonly List<MenuItemDto> _selectedItems;

    public ConfirmOrderPage(ApiService apiService, MenuDto menu, List<MenuItemDto> selectedItems)
    {
        InitializeComponent();
        _apiService    = apiService;
        _menu          = menu;
        _selectedItems = selectedItems;
        LoadSummary();
    }

    private void LoadSummary()
    {
        OrderItemsCollection.ItemsSource = _selectedItems;
        var total = _selectedItems.Sum(i => i.ItemTotal);
        TotalLabel.Text = $"Rs.{total:0}";
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

    private async void PlaceOrderButton_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Place Order",
            "Are you sure you want to place this order?", "Yes, Place Order", "Cancel");
        if (!confirm) return;

        PlaceOrderButton.IsEnabled = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            var request = new CreateOrderRequest
            {
                MenuId = _menu.MenuId,
                Items  = _selectedItems.Select(i => new CreateOrderItemRequest
                {
                    MenuItemId = i.MenuItemId,
                    Quantity   = i.Quantity
                }).ToList()
            };

            var (success, message) = await _apiService.PlaceOrderAsync(request);

            if (success)
            {
                await DisplayAlert("Order Placed!",
                    "Your order has been placed successfully!\nSandeep daa will prepare it soon!", "OK");
                await Navigation.PopToRootAsync();
            }
            else
            {
                await DisplayAlert("Error", message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Connection failed: {ex.Message}", "OK");
        }
        finally
        {
            PlaceOrderButton.IsEnabled = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }
}