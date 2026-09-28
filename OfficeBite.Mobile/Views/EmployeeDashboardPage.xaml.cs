using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class EmployeeDashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private MenuDto? _todayMenu;
    private List<MenuItemDto> _menuItems = new();

    public EmployeeDashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPageAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadPageAsync();
        RefreshView.IsRefreshing = false;
    }

    private async Task LoadPageAsync()
    {
        var name = await AuthHelper.GetUserNameAsync();
        UserNameLabel.Text = name;
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        var hour = DateTime.Now.Hour;
        GreetingLabel.Text = hour < 12 ? "Good Morning! " : hour < 17 ? "Good Afternoon! " : "Good Evening! ";
        await LoadMenuAsync();
    }

    private async Task LoadMenuAsync()
    {
        try
        {
            _todayMenu = await _apiService.GetTodayMenuAsync();

            if (_todayMenu == null)
            {
                NoMenuBorder.IsVisible            = true;
                MenuCard.IsVisible                = false;
                AlreadyOrderedCard.IsVisible      = false;
                OrderExecutionSection.IsVisible   = false;
                CartBar.IsVisible                 = false;
                return;
            }

            NoMenuBorder.IsVisible = false;
            MenuCard.IsVisible     = true;
            MenuTitleLabel.Text    = _todayMenu.Title;

            OrderingBadge.BackgroundColor = Color.FromArgb("#10B981");
            OrderingBadgeLabel.Text       = "Available Today";

            // Check if user already placed any order today
            var myOrders = await _apiService.GetMyOrdersAsync();
            var todayOrder = myOrders.FirstOrDefault(o => o.MenuId == _todayMenu.MenuId && o.Status != "Cancelled");

            if (todayOrder != null)
            {
                AlreadyOrderedCard.IsVisible = true;
                MyOrderStatusBadge.BackgroundColor = todayOrder.StatusColor;
                MyOrderStatusLabel.Text = todayOrder.StatusEmoji;
                MyOrderTotalLabel.Text = todayOrder.TotalDisplay;

                var itemDescriptions = todayOrder.Items.Select(i => $"• {i.FoodName} x {i.Quantity} ({i.PriceDisplay})").ToList();
                if (!string.IsNullOrWhiteSpace(todayOrder.SpecialInstructions))
                {
                    itemDescriptions.Add($"📝 Note: \"{todayOrder.SpecialInstructions}\"");
                }
                MyOrderDetailsLabel.Text = string.Join("\n", itemDescriptions);
            }
            else
            {
                AlreadyOrderedCard.IsVisible = false;
            }

            // Build menu items list
            _menuItems = _todayMenu.Items
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
            UpdateCartBar();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load menu: {ex.Message}", "OK");
        }
    }

    private void RenderFoodItems()
    {
        FoodItemsListStack.Children.Clear();

        foreach (var item in _menuItems)
        {
            var card = BuildMenuItemCard(item);
            FoodItemsListStack.Children.Add(card);
        }
    }

    private View BuildMenuItemCard(MenuItemDto item)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(14, 12)
        };

        // Left info stack
        var infoStack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        var nameLabel = new Label
        {
            Text = item.FoodName,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A")
        };
        var priceLabel = new Label
        {
            Text = $"Rs. {item.Price:0}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#4F46E5")
        };
        infoStack.Children.Add(nameLabel);
        infoStack.Children.Add(priceLabel);

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            var descLabel = new Label
            {
                Text = item.Description,
                FontSize = 12,
                TextColor = Color.FromArgb("#64748B")
            };
            infoStack.Children.Add(descLabel);
        }

        // Subtotal indicator when qty > 0
        var subtotalLabel = new Label
        {
            Text = $"Subtotal: Rs.{item.ItemTotal:0}",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#16A34A"),
            IsVisible = item.Quantity > 0
        };
        infoStack.Children.Add(subtotalLabel);

        // Right side container
        var rightContainer = new ContentView { VerticalOptions = LayoutOptions.Center };

        void RefreshCardState()
        {
            subtotalLabel.IsVisible = item.Quantity > 0;
            subtotalLabel.Text = $"Subtotal: Rs.{item.ItemTotal:0}";

            if (item.Quantity == 0)
            {
                // Show "+ Add" button
                var addBtn = new Button
                {
                    Text = "+ Add to Cart",
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#EDE9FE"),
                    TextColor = Color.FromArgb("#6D28D9"),
                    CornerRadius = 10,
                    HeightRequest = 38,
                    Padding = new Thickness(12, 0)
                };

                addBtn.Clicked += (s, e) =>
                {
                    item.Quantity = 1;
                    RefreshCardState();
                    UpdateCartBar();
                };

                rightContainer.Content = addBtn;
            }
            else
            {
                // Show [-] [qty] [+] counter
                var qtyGrid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(34) },
                        new ColumnDefinition { Width = new GridLength(32) },
                        new ColumnDefinition { Width = new GridLength(34) }
                    },
                    VerticalOptions = LayoutOptions.Center
                };

                var minusBtn = new Button
                {
                    Text = "−",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#FEE2E2"),
                    TextColor = Color.FromArgb("#DC2626"),
                    HeightRequest = 34,
                    WidthRequest = 34,
                    CornerRadius = 8,
                    Padding = new Thickness(0)
                };

                var qtyLabel = new Label
                {
                    Text = item.Quantity.ToString(),
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#0F172A"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                var plusBtn = new Button
                {
                    Text = "+",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    BackgroundColor = Color.FromArgb("#4F46E5"),
                    TextColor = Colors.White,
                    HeightRequest = 34,
                    WidthRequest = 34,
                    CornerRadius = 8,
                    Padding = new Thickness(0)
                };

                minusBtn.Clicked += (s, e) =>
                {
                    if (item.Quantity > 0)
                    {
                        item.Quantity--;
                        RefreshCardState();
                        UpdateCartBar();
                    }
                };

                plusBtn.Clicked += (s, e) =>
                {
                    item.Quantity++;
                    RefreshCardState();
                    UpdateCartBar();
                };

                Grid.SetColumn(minusBtn, 0);
                Grid.SetColumn(qtyLabel, 1);
                Grid.SetColumn(plusBtn, 2);
                qtyGrid.Children.Add(minusBtn);
                qtyGrid.Children.Add(qtyLabel);
                qtyGrid.Children.Add(plusBtn);

                rightContainer.Content = qtyGrid;
            }
        }

        RefreshCardState();

        Grid.SetColumn(infoStack, 0);
        Grid.SetColumn(rightContainer, 1);
        grid.Children.Add(infoStack);
        grid.Children.Add(rightContainer);

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Stroke = Color.FromArgb("#E2E8F0"),
            StrokeThickness = 1,
            Padding = new Thickness(2)
        };
        border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(14)
        };
        border.Content = grid;

        return border;
    }

    private void UpdateCartBar()
    {
        var selected = _menuItems.Where(i => i.Quantity > 0).ToList();
        var totalCount = selected.Sum(i => i.Quantity);
        var totalPrice = selected.Sum(i => i.ItemTotal);

        if (totalCount > 0)
        {
            OrderExecutionSection.IsVisible = true;
            CartBar.IsVisible = true;

            var itemsSummaryText = $"{totalCount} item{(totalCount == 1 ? "" : "s")} selected";
            var totalDisplay = $"Rs. {totalPrice:0}";

            InlineItemCountLabel.Text = itemsSummaryText;
            InlineTotalLabel.Text     = $"Total: {totalDisplay}";
            InlinePlaceOrderBtn.Text  = $"🚀 Place Order ({totalDisplay})";

            CartSummaryLabel.Text = itemsSummaryText;
            CartTotalLabel.Text   = totalDisplay;
            PlaceOrderButton.Text = $"🚀 Place Order ({totalDisplay})";
        }
        else
        {
            OrderExecutionSection.IsVisible = false;
            CartBar.IsVisible = false;
        }
    }

    private async void PlaceOrderButton_Clicked(object sender, EventArgs e)
    {
        if (_todayMenu == null) return;

        var selected = _menuItems.Where(i => i.Quantity > 0).ToList();
        if (selected.Count == 0)
        {
            await DisplayAlert("No Items Selected", "Please add at least 1 food item to your order.", "OK");
            return;
        }

        var totalCount = selected.Sum(i => i.Quantity);
        var totalPrice = selected.Sum(i => i.ItemTotal);
        var instructions = SpecialInstructionsEntry.Text?.Trim();

        var summaryItems = string.Join("\n", selected.Select(i => $"• {i.FoodName} x {i.Quantity} = Rs.{i.ItemTotal:0}"));
        var noteText = !string.IsNullOrWhiteSpace(instructions) ? $"\n\n📝 Special Instructions: \"{instructions}\"" : "";

        bool confirm = await DisplayAlert("Confirm Today's Order",
            $"{summaryItems}\n\nTotal Price: Rs.{totalPrice:0}{noteText}\n\nPlace this order now?",
            "Yes, Place Order", "Cancel");

        if (!confirm) return;

        PlaceOrderButton.IsEnabled    = false;
        InlinePlaceOrderBtn.IsEnabled = false;

        try
        {
            var req = new CreateOrderRequest
            {
                MenuId              = _todayMenu.MenuId,
                SpecialInstructions = instructions,
                Items               = selected.Select(i => new CreateOrderItemRequest
                {
                    MenuItemId = i.MenuItemId,
                    Quantity   = i.Quantity
                }).ToList()
            };

            var (success, message) = await _apiService.PlaceOrderAsync(req);

            if (success)
            {
                await DisplayAlert("🎉 Order Placed!", $"Your order of Rs.{totalPrice:0} with special instructions has been sent to the kitchen.", "OK");
                SpecialInstructionsEntry.Text = string.Empty;
                await LoadMenuAsync();
            }
            else
            {
                await DisplayAlert("Order Failed", message, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Connection error: {ex.Message}", "OK");
        }
        finally
        {
            PlaceOrderButton.IsEnabled    = true;
            InlinePlaceOrderBtn.IsEnabled = true;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        Dispatcher.Dispatch(async () =>
        {
            bool exit = await DisplayAlert("Exit OfficeBite", "Do you want to close the app?", "Exit", "Stay");
            if (exit)
            {
#if ANDROID
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
#else
                Application.Current?.Quit();
#endif
            }
        });
        return true;
    }

    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes, Logout", "Cancel");
        if (!confirm) return;
        AuthHelper.Logout();
        Application.Current!.MainPage = new NavigationPage(
            new LoginPage(Handler!.MauiContext!.Services.GetRequiredService<ApiService>()))
        {
            BarBackgroundColor = Color.FromArgb("#4F46E5"),
            BarTextColor       = Colors.White
        };
    }
}