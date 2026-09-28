using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class AdminManualOrderPage : ContentPage
{
    private readonly ApiService _apiService;
    private MenuDto? _menu;
    private List<EmployeeLookupDto> _employees = new();
    private List<MenuItemDto> _menuItems = new();
    private EmployeeLookupDto? _selectedEmployee;

    public AdminManualOrderPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        MenuLoadingIndicator.IsRunning = true;
        MenuLoadingIndicator.IsVisible = true;

        try
        {
            // 1. Load Employees
            _employees = await _apiService.GetEmployeesAsync();
            EmployeePicker.ItemsSource = null;
            EmployeePicker.ItemsSource = _employees;

            // 2. Load Today's Menu
            _menu = await _apiService.GetAdminTodayMenuAsync() ?? await _apiService.GetTodayMenuAsync();

            if (_menu == null || _menu.Items.Count == 0)
            {
                NoMenuBorder.IsVisible = true;
                FoodItemsListStack.Children.Clear();
                MenuDateBadgeLabel.Text = "No Menu";
            }
            else
            {
                NoMenuBorder.IsVisible = false;
                MenuDateBadgeLabel.Text = _menu.MenuDate;

                _menuItems = _menu.Items
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

                RenderFoodItems();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
        }
        finally
        {
            MenuLoadingIndicator.IsRunning = false;
            MenuLoadingIndicator.IsVisible = false;
            UpdateSummaryBar();
        }
    }

    private void RenderFoodItems()
    {
        FoodItemsListStack.Children.Clear();

        foreach (var item in _menuItems)
        {
            var card = new Border
            {
                BackgroundColor = Colors.White,
                StrokeThickness = 0,
                Padding = new Thickness(16),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 }
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowSpacing = 6
            };

            // Food Name & Price
            var nameLabel = new Label
            {
                Text = item.FoodName,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A")
            };
            var priceLabel = new Label
            {
                Text = item.PriceDisplay,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7C3AED"),
                VerticalOptions = LayoutOptions.Center
            };

            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(priceLabel, 0);
            Grid.SetColumn(priceLabel, 1);
            grid.Children.Add(nameLabel);
            grid.Children.Add(priceLabel);

            // Description
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                var descLabel = new Label
                {
                    Text = item.Description,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#64748B")
                };
                Grid.SetRow(descLabel, 1);
                Grid.SetColumn(descLabel, 0);
                Grid.SetColumnSpan(descLabel, 2);
                grid.Children.Add(descLabel);
            }

            // Plus / Minus Controls
            var controlsGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                Margin = new Thickness(0, 4, 0, 0)
            };

            var minusBtn = new Button
            {
                Text = "−",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                TextColor = Color.FromArgb("#7C3AED")
            };

            var qtyLabel = new Label
            {
                Text = item.Quantity.ToString(),
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A"),
                WidthRequest = 40,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var plusBtn = new Button
            {
                Text = "+",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 40,
                HeightRequest = 40,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#7C3AED"),
                TextColor = Colors.White
            };

            minusBtn.Clicked += (s, e) =>
            {
                if (item.Quantity > 0)
                {
                    item.Quantity--;
                    qtyLabel.Text = item.Quantity.ToString();
                    UpdateSummaryBar();
                }
            };

            plusBtn.Clicked += (s, e) =>
            {
                item.Quantity++;
                qtyLabel.Text = item.Quantity.ToString();
                UpdateSummaryBar();
            };

            Grid.SetColumn(minusBtn, 0);
            Grid.SetColumn(qtyLabel, 1);
            Grid.SetColumn(plusBtn, 2);
            controlsGrid.Children.Add(minusBtn);
            controlsGrid.Children.Add(qtyLabel);
            controlsGrid.Children.Add(plusBtn);

            Grid.SetRow(controlsGrid, 2);
            Grid.SetColumn(controlsGrid, 0);
            Grid.SetColumnSpan(controlsGrid, 2);
            grid.Children.Add(controlsGrid);

            card.Content = grid;
            FoodItemsListStack.Children.Add(card);
        }
    }

    private void EmployeePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (EmployeePicker.SelectedIndex >= 0 && EmployeePicker.SelectedIndex < _employees.Count)
        {
            _selectedEmployee = _employees[EmployeePicker.SelectedIndex];
            SelectedEmployeeInfoLabel.Text = $"✓ Selected: {_selectedEmployee.FullName} ({_selectedEmployee.Email})";
            SelectedEmployeeInfoLabel.IsVisible = true;
        }
        else
        {
            _selectedEmployee = null;
            SelectedEmployeeInfoLabel.IsVisible = false;
        }

        UpdateSummaryBar();
    }

    private void UpdateSummaryBar()
    {
        var selectedItems = _menuItems.Where(i => i.Quantity > 0).ToList();
        var totalAmount   = selectedItems.Sum(i => i.ItemTotal);
        var totalCount    = selectedItems.Sum(i => i.Quantity);

        CartTotalLabel.Text = $"Rs.{totalAmount:0}  ({totalCount} item{(totalCount == 1 ? "" : "s")})";

        bool isValid = _selectedEmployee != null && selectedItems.Count > 0 && _menu != null;

        PlaceOrderButton.IsEnabled = isValid;
        PlaceOrderButton.BackgroundColor = isValid
            ? Color.FromArgb("#7C3AED")
            : Color.FromArgb("#9CA3AF");

        PlaceOrderButton.Text = _selectedEmployee != null && selectedItems.Count > 0
            ? $"Place Order ({_selectedEmployee.FullName})"
            : "Place Order";
    }

    private async void PlaceOrderButton_Clicked(object sender, EventArgs e)
    {
        if (_selectedEmployee == null)
        {
            await DisplayAlert("Select Employee", "Please select an employee first.", "OK");
            return;
        }

        if (_menu == null)
        {
            await DisplayAlert("Error", "Today's menu is not available.", "OK");
            return;
        }

        var selectedItems = _menuItems.Where(i => i.Quantity > 0).ToList();
        if (selectedItems.Count == 0)
        {
            await DisplayAlert("Empty Order", "Please select at least one food item.", "OK");
            return;
        }

        var itemsSummary = string.Join("\n", selectedItems.Select(i => $"• {i.FoodName} x {i.Quantity} = Rs.{i.ItemTotal:0}"));
        var total = selectedItems.Sum(i => i.ItemTotal);

        bool confirm = await DisplayAlert(
            "Confirm Manual Order",
            $"Employee: {_selectedEmployee.FullName}\n\nItems:\n{itemsSummary}\n\nTotal: Rs.{total:0}\n\nPlace this order now?",
            "Yes, Place Order",
            "Cancel");

        if (!confirm) return;

        PlaceOrderButton.IsEnabled = false;
        PlaceOrderButton.Text = "Placing order...";

        try
        {
            var request = new AdminCreateOrderRequest
            {
                UserId = _selectedEmployee.UserId,
                MenuId = _menu.MenuId,
                SpecialInstructions = SpecialInstructionsEditor.Text?.Trim(),
                Items = selectedItems.Select(i => new CreateOrderItemRequest
                {
                    MenuItemId = i.MenuItemId,
                    Quantity   = i.Quantity
                }).ToList()
            };

            var (success, msg) = await _apiService.PlaceAdminManualOrderAsync(request);

            if (success)
            {
                bool another = await DisplayAlert(
                    "Order Placed! 🎉",
                    $"Manual order successfully placed for {_selectedEmployee.FullName}!\nTotal: Rs.{total:0}",
                    "Take Another Order",
                    "Back to Dashboard");

                if (another)
                {
                    // Reset form for next order
                    EmployeePicker.SelectedIndex = -1;
                    _selectedEmployee = null;
                    SelectedEmployeeInfoLabel.IsVisible = false;
                    SpecialInstructionsEditor.Text = string.Empty;
                    foreach (var itm in _menuItems) itm.Quantity = 0;
                    RenderFoodItems();
                    UpdateSummaryBar();
                }
                else
                {
                    await Navigation.PopAsync();
                }
            }
            else
            {
                await DisplayAlert("Failed to Place Order", msg, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not place order: {ex.Message}", "OK");
        }
        finally
        {
            UpdateSummaryBar();
        }
    }

    private async void BackButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
