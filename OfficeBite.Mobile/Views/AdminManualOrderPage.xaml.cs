using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public class CustomDishItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FoodName { get; set; } = string.Empty;
    public decimal Price { get; set; } = 50;
    public int Quantity { get; set; } = 1;
    public decimal ItemTotal => Price * Quantity;
}

public partial class AdminManualOrderPage : ContentPage
{
    private readonly ApiService _apiService;
    private MenuDto? _menu;
    private List<MenuItemDto> _menuItems = new();
    private List<CustomDishItem> _customDishes = new();

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
            // Load Today's Menu
            _menu = await _apiService.GetAdminTodayMenuAsync() ?? await _apiService.GetTodayMenuAsync();

            if (_menu == null || _menu.Items.Count == 0)
            {
                FoodItemsListStack.Children.Clear();
                MenuDateBadgeLabel.Text = "No Menu";
            }
            else
            {
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
            await DisplayAlert("Error", $"Failed to load menu: {ex.Message}", "OK");
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
                Padding = new Thickness(14),
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

            // Food Name
            var nameLabel = new Label
            {
                Text = item.FoodName,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A")
            };

            // Price Box (Editable entry)
            var priceContainer = new HorizontalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            priceContainer.Children.Add(new Label
            {
                Text = "Rs.",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7C3AED"),
                VerticalOptions = LayoutOptions.Center
            });

            var priceEntry = new Entry
            {
                Text = item.Price.ToString("0.##"),
                Keyboard = Keyboard.Numeric,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7C3AED"),
                WidthRequest = 60,
                HeightRequest = 36,
                VerticalOptions = LayoutOptions.Center
            };
            priceEntry.TextChanged += (s, e) =>
            {
                if (decimal.TryParse(priceEntry.Text, out var newPrice) && newPrice >= 0)
                {
                    item.Price = newPrice;
                    UpdateSummaryBar();
                }
            };
            priceContainer.Children.Add(priceEntry);

            Grid.SetRow(nameLabel, 0);
            Grid.SetColumn(nameLabel, 0);
            Grid.SetRow(priceContainer, 0);
            Grid.SetColumn(priceContainer, 1);
            grid.Children.Add(nameLabel);
            grid.Children.Add(priceContainer);

            // Description
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                var descLabel = new Label
                {
                    Text = item.Description,
                    FontSize = 11,
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
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 36,
                HeightRequest = 36,
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                TextColor = Color.FromArgb("#7C3AED"),
                Padding = 0
            };

            var qtyLabel = new Label
            {
                Text = item.Quantity.ToString(),
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A"),
                WidthRequest = 36,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var plusBtn = new Button
            {
                Text = "+",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 36,
                HeightRequest = 36,
                CornerRadius = 8,
                BackgroundColor = Color.FromArgb("#7C3AED"),
                TextColor = Colors.White,
                Padding = 0
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

    private void AddCustomDishButton_Clicked(object sender, EventArgs e)
    {
        var custom = new CustomDishItem();
        _customDishes.Add(custom);
        RenderCustomDishes();
        UpdateSummaryBar();
    }

    private void RenderCustomDishes()
    {
        CustomDishesListStack.Children.Clear();

        foreach (var item in _customDishes)
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                Stroke = Color.FromArgb("#E2E8F0"),
                StrokeThickness = 1.5,
                Padding = new Thickness(12),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
            };

            var stack = new VerticalStackLayout { Spacing = 8 };

            // Item Name Entry & Delete Button
            var topGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

            var nameEntry = new Entry
            {
                Text = item.FoodName,
                Placeholder = "Item Name (e.g. Roti, Paneer, Juice)...",
                PlaceholderColor = Color.FromArgb("#94A3B8"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#0F172A")
            };
            nameEntry.TextChanged += (s, e) =>
            {
                item.FoodName = nameEntry.Text ?? string.Empty;
                UpdateSummaryBar();
            };

            var deleteBtn = new Button
            {
                Text = "🗑️",
                FontSize = 13,
                BackgroundColor = Color.FromArgb("#FEE2E2"),
                TextColor = Color.FromArgb("#DC2626"),
                WidthRequest = 36,
                HeightRequest = 36,
                CornerRadius = 8,
                Padding = 0
            };
            deleteBtn.Clicked += (s, e) =>
            {
                _customDishes.Remove(item);
                RenderCustomDishes();
                UpdateSummaryBar();
            };

            Grid.SetColumn(nameEntry, 0);
            Grid.SetColumn(deleteBtn, 1);
            topGrid.Children.Add(nameEntry);
            topGrid.Children.Add(deleteBtn);
            stack.Children.Add(topGrid);

            // Price Box and Quantity Stepper
            var bottomGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8,
                VerticalOptions = LayoutOptions.Center
            };

            // Price entry
            var priceContainer = new HorizontalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            priceContainer.Children.Add(new Label { Text = "Rs.", FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7C3AED"), VerticalOptions = LayoutOptions.Center });
            var priceEntry = new Entry
            {
                Text = item.Price.ToString("0.##"),
                Keyboard = Keyboard.Numeric,
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#7C3AED"),
                WidthRequest = 60,
                HeightRequest = 36,
                VerticalOptions = LayoutOptions.Center
            };
            priceEntry.TextChanged += (s, e) =>
            {
                if (decimal.TryParse(priceEntry.Text, out var newPrice) && newPrice >= 0)
                {
                    item.Price = newPrice;
                    UpdateSummaryBar();
                }
            };
            priceContainer.Children.Add(priceEntry);

            // Stepper
            var stepper = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
            var minusBtn = new Button { Text = "−", WidthRequest = 32, HeightRequest = 32, CornerRadius = 6, BackgroundColor = Color.FromArgb("#F1F5F9"), TextColor = Color.FromArgb("#7C3AED"), Padding = 0 };
            var qtyLabel = new Label { Text = item.Quantity.ToString(), WidthRequest = 30, HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#0F172A") };
            var plusBtn = new Button { Text = "+", WidthRequest = 32, HeightRequest = 32, CornerRadius = 6, BackgroundColor = Color.FromArgb("#7C3AED"), TextColor = Colors.White, Padding = 0 };

            minusBtn.Clicked += (s, e) =>
            {
                if (item.Quantity > 1)
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

            stepper.Children.Add(minusBtn);
            stepper.Children.Add(qtyLabel);
            stepper.Children.Add(plusBtn);

            Grid.SetColumn(priceContainer, 0);
            Grid.SetColumn(stepper, 2);
            bottomGrid.Children.Add(priceContainer);
            bottomGrid.Children.Add(stepper);
            stack.Children.Add(bottomGrid);

            card.Content = stack;
            CustomDishesListStack.Children.Add(card);
        }
    }

    private void EmployeeNameEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSummaryBar();
    }

    private void UpdateSummaryBar()
    {
        var menuSelected = _menuItems.Where(i => i.Quantity > 0).ToList();
        var customSelected = _customDishes.Where(c => c.Quantity > 0 && !string.IsNullOrWhiteSpace(c.FoodName)).ToList();

        decimal menuTotal = menuSelected.Sum(i => i.Price * i.Quantity);
        decimal customTotal = customSelected.Sum(c => c.Price * c.Quantity);
        decimal totalAmount = menuTotal + customTotal;

        int totalCount = menuSelected.Sum(i => i.Quantity) + customSelected.Sum(c => c.Quantity);

        CartTotalLabel.Text = $"Rs.{totalAmount:0}  ({totalCount} item{(totalCount == 1 ? "" : "s")})";

        string empName = EmployeeNameEntry?.Text?.Trim() ?? string.Empty;
        bool isValid = !string.IsNullOrWhiteSpace(empName) && totalCount > 0;

        PlaceOrderButton.IsEnabled = isValid;
        PlaceOrderButton.BackgroundColor = isValid
            ? Color.FromArgb("#7C3AED")
            : Color.FromArgb("#9CA3AF");

        PlaceOrderButton.Text = !string.IsNullOrWhiteSpace(empName) && totalCount > 0
            ? $"Place Order ({empName})"
            : "Place Order";
    }

    private async void PlaceOrderButton_Clicked(object sender, EventArgs e)
    {
        string empName = EmployeeNameEntry?.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(empName))
        {
            await DisplayAlert("Employee Name", "Please type the employee name first.", "OK");
            return;
        }

        var menuSelected = _menuItems.Where(i => i.Quantity > 0).ToList();
        var customSelected = _customDishes.Where(c => c.Quantity > 0 && !string.IsNullOrWhiteSpace(c.FoodName)).ToList();

        if (menuSelected.Count == 0 && customSelected.Count == 0)
        {
            await DisplayAlert("Empty Order", "Please select or add at least one item.", "OK");
            return;
        }

        var summaryList = new List<string>();
        foreach (var m in menuSelected)
        {
            summaryList.Add($"• {m.FoodName} x {m.Quantity} = Rs.{m.Price * m.Quantity:0}");
        }
        foreach (var c in customSelected)
        {
            summaryList.Add($"• {c.FoodName} x {c.Quantity} = Rs.{c.Price * c.Quantity:0}");
        }

        decimal total = menuSelected.Sum(i => i.Price * i.Quantity) + customSelected.Sum(c => c.Price * c.Quantity);
        var itemsSummary = string.Join("\n", summaryList);

        bool confirm = await DisplayAlert(
            "Confirm Manual Order",
            $"Employee: {empName}\n\nItems:\n{itemsSummary}\n\nTotal: Rs.{total:0}\n\nPlace this order now?",
            "Yes, Place Order",
            "Cancel");

        if (!confirm) return;

        PlaceOrderButton.IsEnabled = false;
        PlaceOrderButton.Text = "Placing order...";

        try
        {
            // 1. Resolve or auto-register employee so UserId is always valid
            int targetUserId = 0;
            try
            {
                var employees = await _apiService.GetEmployeesAsync();
                var matched = employees.FirstOrDefault(e => e.FullName.Equals(empName, StringComparison.OrdinalIgnoreCase));
                if (matched != null)
                {
                    targetUserId = matched.UserId;
                }
                else
                {
                    var slug = System.Text.RegularExpressions.Regex.Replace(empName.ToLower(), @"[^a-z0-9]", ".");
                    slug = slug.Trim('.');
                    if (string.IsNullOrEmpty(slug)) slug = "employee";
                    var autoEmail = $"{slug}.{Random.Shared.Next(100, 999)}@officebite.local";

                    await _apiService.RegisterAsync(new RegisterRequest
                    {
                        FullName = empName,
                        Email = autoEmail,
                        Mobile = "9999999999",
                        Password = "Employee@123"
                    });

                    var updatedEmployees = await _apiService.GetEmployeesAsync();
                    var newlyRegistered = updatedEmployees.FirstOrDefault(e => e.Email == autoEmail)
                        ?? updatedEmployees.FirstOrDefault(e => e.FullName.Equals(empName, StringComparison.OrdinalIgnoreCase))
                        ?? updatedEmployees.LastOrDefault();

                    if (newlyRegistered != null)
                    {
                        targetUserId = newlyRegistered.UserId;
                    }
                }
            }
            catch { }

            // 2. Add custom dishes to menu so they have MenuItemIds
            if (customSelected.Any() && _menu != null)
            {
                try
                {
                    var newMenuItems = customSelected.Select(c => new CreateMenuItemRequest
                    {
                        FoodName = c.FoodName.Trim(),
                        Price = c.Price,
                        Description = "Manual item"
                    }).ToList();

                    await _apiService.AddMenuItemsAsync(_menu.MenuId, newMenuItems);
                    var refreshedMenu = await _apiService.GetAdminTodayMenuAsync() ?? await _apiService.GetTodayMenuAsync();
                    if (refreshedMenu != null)
                    {
                        _menu = refreshedMenu;
                    }
                }
                catch { }
            }

            // 3. Prepare items list
            var itemsList = new List<CreateOrderItemRequest>();

            foreach (var m in menuSelected)
            {
                itemsList.Add(new CreateOrderItemRequest
                {
                    MenuItemId = m.MenuItemId,
                    FoodName = m.FoodName,
                    UnitPrice = m.Price,
                    Quantity = m.Quantity
                });
            }

            foreach (var c in customSelected)
            {
                var matchingMenuItem = _menu?.Items.FirstOrDefault(i => i.FoodName.Equals(c.FoodName.Trim(), StringComparison.OrdinalIgnoreCase));
                int menuItemId = matchingMenuItem?.MenuItemId ?? 0;

                itemsList.Add(new CreateOrderItemRequest
                {
                    MenuItemId = menuItemId,
                    FoodName = c.FoodName.Trim(),
                    UnitPrice = c.Price,
                    Quantity = c.Quantity
                });
            }

            var request = new AdminCreateOrderRequest
            {
                UserId = targetUserId,
                EmployeeName = empName,
                MenuId = _menu?.MenuId ?? 0,
                SpecialInstructions = SpecialInstructionsEditor.Text?.Trim(),
                Items = itemsList
            };

            var (success, msg) = await _apiService.PlaceAdminManualOrderAsync(request);

            if (success)
            {
                bool another = await DisplayAlert(
                    "Order Placed! 🎉",
                    $"Manual order successfully placed for {empName}!\nTotal: Rs.{total:0}",
                    "Take Another Order",
                    "Back to Dashboard");

                if (another)
                {
                    EmployeeNameEntry.Text = string.Empty;
                    SpecialInstructionsEditor.Text = string.Empty;
                    foreach (var itm in _menuItems) itm.Quantity = 0;
                    _customDishes.Clear();
                    RenderFoodItems();
                    RenderCustomDishes();
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
}
