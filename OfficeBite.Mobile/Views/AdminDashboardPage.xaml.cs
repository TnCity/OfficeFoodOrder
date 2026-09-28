using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class AdminDashboardPage : ContentPage
{
    private readonly ApiService _apiService;

    public AdminDashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DateLabel.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        await LoadSummaryAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadSummaryAsync();
        RefreshView.IsRefreshing = false;
    }

    private async Task LoadSummaryAsync()
    {
        try
        {
            var summary = await _apiService.GetTodaySummaryAsync();
            if (summary == null)
            {
                ResetStats();
            }
            else
            {
                TotalOrdersLabel.Text = summary.TotalOrders.ToString();
                TotalAmountLabel.Text = summary.TotalDisplay;
                PendingLabel.Text     = summary.PendingOrders.ToString();
                ConfirmedLabel.Text   = summary.ConfirmedOrders.ToString();
                DeliveredLabel.Text   = summary.DeliveredOrders.ToString();
                CancelledLabel.Text   = summary.CancelledOrders.ToString();
            }

            // Load today's live orders list
            await LoadLiveOrdersAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load summary: {ex.Message}", "OK");
        }
    }

    private async Task LoadLiveOrdersAsync()
    {
        try
        {
            var orders = await _apiService.GetTodayOrdersAsync();
            LiveOrdersStack.Children.Clear();

            if (orders.Count == 0)
            {
                NoLiveOrdersBorder.IsVisible = true;
                LiveOrdersStack.IsVisible    = false;
                LiveOrdersCountLabel.Text    = "0 orders";
                return;
            }

            NoLiveOrdersBorder.IsVisible = false;
            LiveOrdersStack.IsVisible    = true;
            LiveOrdersCountLabel.Text    = $"{orders.Count} order{(orders.Count == 1 ? "" : "s")}";

            foreach (var order in orders)
            {
                var card = BuildOrderCard(order);
                LiveOrdersStack.Children.Add(card);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminDashboard] Live orders error: {ex.Message}");
        }
    }

    private View BuildOrderCard(OrderDto order)
    {
        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = new Thickness(16, 14)
        };
        border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(16)
        };
        border.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#08000000")),
            Offset = new Point(0, 2),
            Radius = 8,
            Opacity = 0.08f
        };

        var stack = new VerticalStackLayout { Spacing = 8 };

        // Header: Employee Name & Total Price
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameStack = new VerticalStackLayout { Spacing = 2 };
        var nameLabel = new Label
        {
            Text = $"👤 {order.UserName}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A")
        };
        nameStack.Children.Add(nameLabel);

        var totalLabel = new Label
        {
            Text = order.TotalDisplay,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#7C3AED"),
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(nameStack, 0);
        Grid.SetColumn(totalLabel, 1);
        headerGrid.Children.Add(nameStack);
        headerGrid.Children.Add(totalLabel);
        stack.Children.Add(headerGrid);

        // Status badge
        var badge = new Border
        {
            BackgroundColor = order.StatusColor,
            StrokeThickness = 0,
            Padding = new Thickness(10, 4),
            HorizontalOptions = LayoutOptions.Start
        };
        badge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
        badge.Content = new Label
        {
            Text = order.StatusEmoji,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };
        stack.Children.Add(badge);

        // Items list
        var itemsStack = new VerticalStackLayout { Spacing = 4, Margin = new Thickness(0, 4, 0, 4) };
        foreach (var item in order.Items)
        {
            var itemGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            var fl = new Label { Text = $"• {item.FoodName} x {item.Quantity}", FontSize = 13, TextColor = Color.FromArgb("#334155") };
            var pl = new Label { Text = item.PriceDisplay, FontSize = 13, TextColor = Color.FromArgb("#64748B") };
            Grid.SetColumn(fl, 0);
            Grid.SetColumn(pl, 1);
            itemGrid.Children.Add(fl);
            itemGrid.Children.Add(pl);
            itemsStack.Children.Add(itemGrid);
        }
        stack.Children.Add(itemsStack);

        // Special instruction note if present
        if (!string.IsNullOrWhiteSpace(order.SpecialInstructions))
        {
            var noteBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#FEF3C7"),
                Stroke = Color.FromArgb("#FDE68A"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 2, 0, 4)
            };
            noteBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(8)
            };
            noteBorder.Content = new Label
            {
                Text = $"📝 Note: \"{order.SpecialInstructions}\"",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B45309")
            };
            stack.Children.Add(noteBorder);
        }

        // Action Buttons according to workflow
        if (order.Status == "Pending")
        {
            stack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 4) });

            var btnGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

            var confirmBtn = new Button
            {
                Text = "✔️ Confirm Order",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#2563EB"),
                TextColor = Colors.White
            };
            confirmBtn.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert("Confirm Order", $"Confirm {order.UserName}'s order?", "Yes, Confirm", "Cancel");
                if (!confirm) return;

                var (success, message) = await _apiService.UpdateOrderStatusAsync(order.OrderId, "Confirmed");
                if (success) await LoadSummaryAsync();
                else await DisplayAlert("Error", message, "OK");
            };

            var cancelBtn = new Button
            {
                Text = "❌ Cancel",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#FEE2E2"),
                TextColor = Color.FromArgb("#DC2626")
            };
            cancelBtn.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert("Cancel Order", $"Cancel {order.UserName}'s order?", "Yes, Cancel", "No");
                if (!confirm) return;

                var (success, message) = await _apiService.UpdateOrderStatusAsync(order.OrderId, "Cancelled");
                if (success) await LoadSummaryAsync();
                else await DisplayAlert("Error", message, "OK");
            };

            Grid.SetColumn(confirmBtn, 0);
            Grid.SetColumn(cancelBtn, 1);
            btnGrid.Children.Add(confirmBtn);
            btnGrid.Children.Add(cancelBtn);
            stack.Children.Add(btnGrid);
        }
        else if (order.Status == "Confirmed")
        {
            stack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#F1F5F9"), Margin = new Thickness(0, 4) });

            var btnGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

            var deliverBtn = new Button
            {
                Text = "🚚 Mark Delivered",
                FontSize = 13,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#059669"),
                TextColor = Colors.White
            };
            deliverBtn.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert("Deliver Order", $"Mark {order.UserName}'s order as Delivered to employee?", "Yes, Delivered", "Cancel");
                if (!confirm) return;

                var (success, message) = await _apiService.UpdateOrderStatusAsync(order.OrderId, "Delivered");
                if (success) await LoadSummaryAsync();
                else await DisplayAlert("Error", message, "OK");
            };

            var cancelBtn = new Button
            {
                Text = "❌ Cancel",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 38,
                CornerRadius = 10,
                BackgroundColor = Color.FromArgb("#FEE2E2"),
                TextColor = Color.FromArgb("#DC2626")
            };
            cancelBtn.Clicked += async (s, e) =>
            {
                bool confirm = await DisplayAlert("Cancel Order", $"Cancel {order.UserName}'s order?", "Yes, Cancel", "No");
                if (!confirm) return;

                var (success, message) = await _apiService.UpdateOrderStatusAsync(order.OrderId, "Cancelled");
                if (success) await LoadSummaryAsync();
                else await DisplayAlert("Error", message, "OK");
            };

            Grid.SetColumn(deliverBtn, 0);
            Grid.SetColumn(cancelBtn, 1);
            btnGrid.Children.Add(deliverBtn);
            btnGrid.Children.Add(cancelBtn);
            stack.Children.Add(btnGrid);
        }

        border.Content = stack;
        return border;
    }

    private void ResetStats()
    {
        TotalOrdersLabel.Text = "0";
        TotalAmountLabel.Text = "Rs.0";
        PendingLabel.Text     = "0";
        ConfirmedLabel.Text   = "0";
        DeliveredLabel.Text   = "0";
        CancelledLabel.Text   = "0";
        NoLiveOrdersBorder.IsVisible = true;
        LiveOrdersStack.IsVisible    = false;
        LiveOrdersCountLabel.Text    = "0 orders";
    }

    private async void CreateMenuButton_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new CreateMenuPage(_apiService));

    private async void ViewOrdersButton_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new TodayOrdersPage(_apiService));

    private async void ManualOrderButton_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new AdminManualOrderPage(_apiService));

    private async void RegisterEmployeeButton_Clicked(object sender, EventArgs e)
        => await Navigation.PushAsync(new RegisterEmployeePage(_apiService));

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
            BarBackgroundColor = Color.FromArgb("#7C3AED"),
            BarTextColor       = Colors.White
        };
    }
}