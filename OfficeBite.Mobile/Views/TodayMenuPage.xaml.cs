using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class TodayMenuPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly MenuDto _menu;
    private List<MenuItemDto> _items = new();

    public TodayMenuPage(ApiService apiService, MenuDto menu)
    {
        InitializeComponent();
        _apiService = apiService;
        _menu = menu;
        LoadMenu();
    }

    private void LoadMenu()
    {
        MenuTitleLabel.Text = _menu.Title;
        _items = _menu.Items
            .Where(i => i.IsAvailable)
            .Select(i => new MenuItemDto
            {
                MenuItemId  = i.MenuItemId,
                FoodName    = i.FoodName,
                Description = i.Description,
                Price       = i.Price,
                IsAvailable = i.IsAvailable,
                Quantity    = 0
            }).ToList();

        FoodItemsCollection.ItemsSource = null;
        FoodItemsCollection.ItemsSource = _items;
        UpdateCartBar();
    }

    private void PlusButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is MenuItemDto item)
        {
            item.Quantity++;
            RefreshItem(item);
            UpdateCartBar();
        }
    }

    private void MinusButton_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is MenuItemDto item)
        {
            if (item.Quantity > 0) item.Quantity--;
            RefreshItem(item);
            UpdateCartBar();
        }
    }

    private void RefreshItem(MenuItemDto item)
    {
        var idx = _items.IndexOf(item);
        if (idx < 0) return;
        _items[idx] = item;
        FoodItemsCollection.ItemsSource = null;
        FoodItemsCollection.ItemsSource = _items;
    }

    private void UpdateCartBar()
    {
        var selected = _items.Where(i => i.Quantity > 0).ToList();
        var total    = selected.Sum(i => i.ItemTotal);
        var count    = selected.Sum(i => i.Quantity);
        CartTotalLabel.Text = $"Rs.{total:0}  ({count} item{(count == 1 ? "" : "s")})";
        ConfirmOrderButton.IsEnabled     = selected.Count > 0;
        ConfirmOrderButton.BackgroundColor = selected.Count > 0
            ? Color.FromArgb("#4F46E5")
            : Color.FromArgb("#9CA3AF");
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

    private async void ConfirmOrderButton_Clicked(object sender, EventArgs e)
    {
        var selected = _items.Where(i => i.Quantity > 0).ToList();
        if (selected.Count == 0) return;
        await Navigation.PushAsync(new ConfirmOrderPage(_apiService, _menu, selected));
    }
}