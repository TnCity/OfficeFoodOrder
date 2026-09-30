using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class EmployeeDashboardPage : ContentPage
{
    private readonly ApiService _apiService;
    private MenuDto? _todayMenu;
    private List<MenuItemDto> _menuItems = new();
    private readonly IDispatcherTimer _liveRefreshTimer;
    private bool _isRefreshingSilently;
    private OrderDto? _activeTodayOrder;
    private bool _isModifyingOrder;

    public EmployeeDashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        _liveRefreshTimer = Dispatcher.CreateTimer();
        _liveRefreshTimer.Interval = TimeSpan.FromSeconds(8);
        _liveRefreshTimer.Tick += async (s, e) =>
        {
            await RefreshTodayOrderStatusSilentlyAsync();
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _liveRefreshTimer.Start();
        await LoadPageAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _liveRefreshTimer.Stop();
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
        DateLabel.Text = DateTimeHelper.FormatIstFullDate();
        var hour = DateTimeHelper.NowIst.Hour;
        GreetingLabel.Text = hour < 12 ? "Good Morning! " : hour < 17 ? "Good Afternoon! " : "Good Evening! ";

        await LoadTodayDataAsync();
    }

    private async Task LoadTodayDataAsync()
    {
        try
        {
            _todayMenu = await _apiService.GetTodayMenuAsync();

            var myOrders = await _apiService.GetMyOrdersAsync();
            var todayOrders = myOrders.Where(o => o.IsToday || (_todayMenu != null && o.MenuId == _todayMenu.MenuId)).ToList();
            _activeTodayOrder = todayOrders.FirstOrDefault(o => o.Status != "Cancelled") ?? todayOrders.FirstOrDefault();

            UpdateTodayOrderUi(_activeTodayOrder);

            if (_todayMenu == null)
            {
                NoMenuBorder.IsVisible          = true;
                MenuCard.IsVisible              = false;
                OrderExecutionSection.IsVisible = false;
                CartBar.IsVisible               = false;
                return;
            }

            NoMenuBorder.IsVisible          = false;
            MenuCard.IsVisible              = true;
            MenuSectionTitleLabel.IsVisible = true;
            MenuTitleLabel.Text             = _todayMenu.Title;

            OrderingBadge.BackgroundColor = Color.FromArgb("#10B981");
            OrderingBadgeLabel.Text       = "Available Today";

            // If not actively modifying, build menu items fresh (only active items shown to employees)
            if (!_isModifyingOrder)
            {
                _menuItems = _todayMenu.Items
                    .Where(i => i.IsAvailable)
                    .Select(i => new MenuItemDto
                    {
                        MenuItemId  = i.MenuItemId,
                        FoodName    = i.FoodName,
                        Description = i.Description,
                        Price       = i.Price,
                        IsAvailable = true,
                        Quantity    = 0
                    }).ToList();

                RenderFoodItems(MenuSearchEntry.Text?.Trim() ?? string.Empty);
                UpdateCartBar();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load data: {ex.Message}", "OK");
        }
    }

    private async Task RefreshTodayOrderStatusSilentlyAsync()
    {
        if (_isRefreshingSilently) return;
        _isRefreshingSilently = true;

        try
        {
            var myOrders = await _apiService.GetMyOrdersAsync();
            var todayOrders = myOrders.Where(o => o.IsToday || (_todayMenu != null && o.MenuId == _todayMenu.MenuId)).ToList();
            var latestOrder = todayOrders.FirstOrDefault(o => o.Status != "Cancelled") ?? todayOrders.FirstOrDefault();

            // If status changed from Pending to Confirmed while user was modifying, cancel modifying mode automatically
            if (_activeTodayOrder?.Status == "Pending" && latestOrder?.Status != "Pending" && _isModifyingOrder)
            {
                _isModifyingOrder = false;
                CancelModifyBtn.IsVisible = false;
                await DisplayAlert("Order Locked", "Admin has accepted your order. Order is now Under process and cannot be changed.", "OK");
            }

            _activeTodayOrder = latestOrder;
            UpdateTodayOrderUi(_activeTodayOrder);
        }
        catch
        {
            // Silently ignore background polling exceptions
        }
        finally
        {
            _isRefreshingSilently = false;
        }
    }

    private void UpdateTodayOrderUi(OrderDto? todayOrder)
    {
        if (todayOrder == null)
        {
            AlreadyOrderedCard.IsVisible = false;
            return;
        }

        AlreadyOrderedCard.IsVisible = true;
        MyOrderStatusBadge.BackgroundColor = todayOrder.StatusColor;
        MyOrderStatusLabel.Text = todayOrder.StatusEmoji;
        MyOrderTotalLabel.Text = todayOrder.TotalDisplay;

        // Status Workflow Logic
        if (todayOrder.Status == "Pending")
        {
            ModifyOrderBtn.IsVisible  = !_isModifyingOrder;
            CancelModifyBtn.IsVisible = _isModifyingOrder;
            MyOrderStatusNoteLabel.Text = "⏳ Waiting for Admin to accept. You can modify items or notes below.";
            MyOrderStatusNoteLabel.TextColor = Color.FromArgb("#B45309");
        }
        else if (todayOrder.Status == "Confirmed")
        {
            // Admin Accepted -> Order is Under Process -> LOCKED (Cannot modify)
            ModifyOrderBtn.IsVisible  = false;
            CancelModifyBtn.IsVisible = false;
            _isModifyingOrder         = false;

            MyOrderStatusBadge.BackgroundColor = Color.FromArgb("#2563EB");
            MyOrderStatusLabel.Text = "👨‍🍳 Order is Under process.";
            MyOrderStatusNoteLabel.Text = "👨‍🍳 Admin has accepted your order. Food is being prepared!";
            MyOrderStatusNoteLabel.TextColor = Color.FromArgb("#1D4ED8");
        }
        else if (todayOrder.Status == "Delivered" || todayOrder.Status == "Completed")
        {
            ModifyOrderBtn.IsVisible  = false;
            CancelModifyBtn.IsVisible = false;
            _isModifyingOrder         = false;

            MyOrderStatusBadge.BackgroundColor = Color.FromArgb("#059669");
            MyOrderStatusLabel.Text = "✅ Delivered";
            MyOrderStatusNoteLabel.Text = "🎉 Order delivered! Enjoy your meal!";
            MyOrderStatusNoteLabel.TextColor = Color.FromArgb("#047857");
        }
        else if (todayOrder.Status == "Cancelled")
        {
            ModifyOrderBtn.IsVisible  = false;
            CancelModifyBtn.IsVisible = false;
            _isModifyingOrder         = false;

            MyOrderStatusBadge.BackgroundColor = Color.FromArgb("#EF4444");
            MyOrderStatusLabel.Text = "❌ Cancelled";
            MyOrderStatusNoteLabel.Text = "❌ Order cancelled by Admin. You can place a new order below.";
            MyOrderStatusNoteLabel.TextColor = Color.FromArgb("#DC2626");
        }
    }

    private void ModifyOrderBtn_Clicked(object sender, EventArgs e)
    {
        if (_todayMenu == null || _activeTodayOrder == null || _activeTodayOrder.Status != "Pending")
        {
            DisplayAlert("Cannot Modify", "Order is already accepted by Admin and cannot be modified.", "OK");
            return;
        }

        _isModifyingOrder = true;
        ModifyOrderBtn.IsVisible  = false;
        CancelModifyBtn.IsVisible = true;

        // Populate menu items with existing order items
        _menuItems = _todayMenu.Items
            .Where(i => i.IsAvailable)
            .Select(i =>
            {
                var existingItem = _activeTodayOrder.Items.FirstOrDefault(oi => oi.MenuItemId == i.MenuItemId);
                return new MenuItemDto
                {
                    MenuItemId  = i.MenuItemId,
                    FoodName    = i.FoodName,
                    Description = i.Description,
                    Price       = i.Price,
                    IsAvailable = i.IsAvailable,
                    Quantity    = existingItem?.Quantity ?? 0
                };
            }).ToList();

        SpecialInstructionsEntry.Text = _activeTodayOrder.SpecialInstructions ?? string.Empty;

        RenderFoodItems();
        UpdateCartBar();

        MenuSectionTitleLabel.Text = "✏️ Change Items in Your Order";
    }

    private void CancelModifyBtn_Clicked(object sender, EventArgs e)
    {
        _isModifyingOrder = false;
        CancelModifyBtn.IsVisible = false;
        MenuSectionTitleLabel.Text = "🍽️ Today's Lunch Menu";

        // Reset menu items to 0
        if (_todayMenu != null)
        {
            _menuItems = _todayMenu.Items
                .Where(i => i.IsAvailable)
                .Select(i => new MenuItemDto
                {
                    MenuItemId  = i.MenuItemId,
                    FoodName    = i.FoodName,
                    Description = i.Description,
                    Price       = i.Price,
                    IsAvailable = true,
                    Quantity    = 0
                }).ToList();

            RenderFoodItems(MenuSearchEntry.Text?.Trim() ?? string.Empty);
            UpdateCartBar();
        }

        UpdateTodayOrderUi(_activeTodayOrder);
    }

    private void MenuSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderFoodItems(e.NewTextValue?.Trim() ?? string.Empty);
    }

    private void RenderFoodItems(string searchQuery = "")
    {
        FoodItemsListStack.Children.Clear();

        var activeItems = _menuItems.Where(i => i.IsAvailable).ToList();

        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? activeItems
            : activeItems.Where(i => i.FoodName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            SearchCountLabel.IsVisible = false;
        }
        else
        {
            SearchCountLabel.IsVisible = true;
            SearchCountLabel.Text = $"Showing {filtered.Count} of {activeItems.Count} items matching \"{searchQuery}\"";
        }

        if (filtered.Count == 0)
        {
            var emptyStack = new VerticalStackLayout
            {
                Spacing = 6,
                Padding = new Thickness(16, 20),
                HorizontalOptions = LayoutOptions.Center
            };
            emptyStack.Children.Add(new Label { Text = "🔍", FontSize = 28, HorizontalOptions = LayoutOptions.Center });
            emptyStack.Children.Add(new Label { Text = string.IsNullOrWhiteSpace(searchQuery) ? "No items available in today's menu" : $"No items match \"{searchQuery}\"", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#64748B"), HorizontalOptions = LayoutOptions.Center });
            FoodItemsListStack.Children.Add(emptyStack);
            return;
        }

        foreach (var item in filtered)
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
            TextColor = item.IsAvailable ? Color.FromArgb("#0F172A") : Color.FromArgb("#94A3B8"),
            TextDecorations = item.IsAvailable ? TextDecorations.None : TextDecorations.Strikethrough
        };
        var priceLabel = new Label
        {
            Text = $"Rs. {item.Price:0}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = item.IsAvailable ? Color.FromArgb("#4F46E5") : Color.FromArgb("#94A3B8")
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
            IsVisible = item.Quantity > 0 && item.IsAvailable
        };
        infoStack.Children.Add(subtotalLabel);

        // Right side container
        var rightContainer = new ContentView { VerticalOptions = LayoutOptions.Center };

        if (!item.IsAvailable)
        {
            // Inactive item: Display Not Available Today badge
            var notAvailBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#F1F5F9"),
                Stroke = Color.FromArgb("#CBD5E1"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 6),
                VerticalOptions = LayoutOptions.Center
            };
            notAvailBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            };
            notAvailBadge.Content = new Label
            {
                Text = "⚪ Not Available Today",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#64748B")
            };
            rightContainer.Content = notAvailBadge;
        }
        else
        {
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
        }

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
            var actionPrefix = _isModifyingOrder ? "💾 Save Changes" : "🚀 Place Order";

            InlineItemCountLabel.Text = itemsSummaryText;
            InlineTotalLabel.Text     = $"Total: {totalDisplay}";
            InlinePlaceOrderBtn.Text  = $"{actionPrefix} ({totalDisplay})";

            CartSummaryLabel.Text = itemsSummaryText;
            CartTotalLabel.Text   = totalDisplay;
            PlaceOrderButton.Text = $"{actionPrefix} ({totalDisplay})";
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

        var promptTitle = _isModifyingOrder ? "Save Changes to Today's Order" : "Confirm Today's Order";
        var confirmBtnText = _isModifyingOrder ? "Yes, Save Changes" : "Yes, Place Order";

        bool confirm = await DisplayAlert(promptTitle,
            $"{summaryItems}\n\nTotal Price: Rs.{totalPrice:0}{noteText}\n\n{(_isModifyingOrder ? "Save these changes now?" : "Place this order now?")}",
            confirmBtnText, "Cancel");

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

            if (_isModifyingOrder && _activeTodayOrder != null && _activeTodayOrder.Status == "Pending")
            {
                // Modifying existing pending order
                var (success, message) = await _apiService.UpdateOrderAsync(_activeTodayOrder.OrderId, req);

                if (success)
                {
                    _isModifyingOrder = false;
                    CancelModifyBtn.IsVisible = false;
                    MenuSectionTitleLabel.Text = "🍽️ Today's Lunch Menu";
                    SpecialInstructionsEntry.Text = string.Empty;
                    await DisplayAlert("🎉 Order Updated!", $"Your changes of Rs.{totalPrice:0} have been saved. Waiting for Admin to accept.", "OK");
                    await LoadTodayDataAsync();
                }
                else
                {
                    await DisplayAlert("Update Failed", message, "OK");
                }
            }
            else
            {
                // Placing a new order
                var (success, message) = await _apiService.PlaceOrderAsync(req);

                if (success)
                {
                    _isModifyingOrder = false;
                    CancelModifyBtn.IsVisible = false;
                    SpecialInstructionsEntry.Text = string.Empty;
                    await DisplayAlert("🎉 Order Placed!", $"Your order of Rs.{totalPrice:0} has been placed. Waiting for Admin to accept.", "OK");
                    await LoadTodayDataAsync();
                }
                else
                {
                    await DisplayAlert("Order Failed", message, "OK");
                }
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

    private async void MyOrdersButton_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new OrderHistoryPage(_apiService));
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
        _liveRefreshTimer.Stop();
        AuthHelper.Logout();
        Application.Current!.MainPage = new NavigationPage(
            new LoginPage(Handler!.MauiContext!.Services.GetRequiredService<ApiService>()))
        {
            BarBackgroundColor = Color.FromArgb("#4F46E5"),
            BarTextColor       = Colors.White
        };
    }
}