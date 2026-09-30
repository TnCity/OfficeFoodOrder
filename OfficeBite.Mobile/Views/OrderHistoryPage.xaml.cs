using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class OrderHistoryPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly IDispatcherTimer _liveTimer;
    private bool _isRefreshingSilently;

    public OrderHistoryPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        _liveTimer = Dispatcher.CreateTimer();
        _liveTimer.Interval = TimeSpan.FromSeconds(8);
        _liveTimer.Tick += async (s, e) =>
        {
            await LoadOrdersSilentlyAsync();
        };
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
        _liveTimer.Start();
        await LoadOrdersAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _liveTimer.Stop();
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

        try
        {
            var orders = await _apiService.GetMyOrdersAsync();
            RenderOrders(orders);
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

    private async Task LoadOrdersSilentlyAsync()
    {
        if (_isRefreshingSilently) return;
        _isRefreshingSilently = true;

        try
        {
            var orders = await _apiService.GetMyOrdersAsync();
            RenderOrders(orders);
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

    private void RenderOrders(List<OrderDto> orders)
    {
        TodayOrdersStack.Children.Clear();
        PastOrdersStack.Children.Clear();

        if (orders.Count == 0)
        {
            NoOrdersBorder.IsVisible     = true;
            TodayOrdersSection.IsVisible = false;
            PastOrdersSection.IsVisible  = false;
            return;
        }

        NoOrdersBorder.IsVisible = false;

        var todayOrders = orders.Where(o => o.IsToday).ToList();
        var pastOrders  = orders.Where(o => !o.IsToday).ToList();

        // If no orders specifically matched IsToday (e.g. timezone edge cases), treat the top one as Today's if placed within 24h
        if (todayOrders.Count == 0 && orders.Count > 0)
        {
            var topOrder = orders.First();
            if (topOrder.CreatedAtDate.HasValue && (DateTimeHelper.NowIst - topOrder.CreatedAtDate.Value).TotalHours < 20)
            {
                todayOrders.Add(topOrder);
                pastOrders.Remove(topOrder);
            }
        }

        // Render Today's Orders
        if (todayOrders.Count > 0)
        {
            TodayOrdersSection.IsVisible = true;
            TodayOrdersCountLabel.Text   = $"{todayOrders.Count} order{(todayOrders.Count == 1 ? "" : "s")}";

            foreach (var order in todayOrders)
            {
                TodayOrdersStack.Children.Add(BuildTodayOrderCard(order));
            }
        }
        else
        {
            TodayOrdersSection.IsVisible = false;
        }

        // Render Past Orders
        if (pastOrders.Count > 0)
        {
            PastOrdersSection.IsVisible = true;
            foreach (var order in pastOrders)
            {
                PastOrdersStack.Children.Add(BuildPastOrderCard(order));
            }
        }
        else
        {
            PastOrdersSection.IsVisible = false;
        }
    }

    private View BuildTodayOrderCard(OrderDto order)
    {
        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = new Thickness(16, 16)
        };
        border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(16)
        };
        border.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#10000000")),
            Offset = new Point(0, 2),
            Radius = 8,
            Opacity = 0.1f
        };

        var stack = new VerticalStackLayout { Spacing = 12 };

        // Header: Time + Total Price + Status Badge
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var infoStack = new VerticalStackLayout { Spacing = 2 };
        var totalLbl = new Label
        {
            Text = order.TotalDisplay,
            FontSize = 20,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#7C3AED")
        };
        var timeLbl = new Label
        {
            Text = string.IsNullOrWhiteSpace(order.TimeFormatted) ? "Today • Live Status ⚡" : $"Placed at {order.TimeFormatted} • Live Status ⚡",
            FontSize = 11,
            TextColor = Color.FromArgb("#64748B")
        };
        infoStack.Children.Add(totalLbl);
        infoStack.Children.Add(timeLbl);

        var badge = new Border
        {
            BackgroundColor = order.StatusColor,
            StrokeThickness = 0,
            Padding = new Thickness(12, 5),
            VerticalOptions = LayoutOptions.Center
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

        Grid.SetColumn(infoStack, 0);
        Grid.SetColumn(badge, 1);
        headerGrid.Children.Add(infoStack);
        headerGrid.Children.Add(badge);
        stack.Children.Add(headerGrid);

        // Step Tracker
        var stepTracker = BuildStepTracker(order.Status);
        stack.Children.Add(stepTracker);

        // Items Stack
        var itemsStack = new VerticalStackLayout { Spacing = 6, Margin = new Thickness(0, 2) };
        foreach (var item in order.Items)
        {
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };
            var name = new Label { Text = $"• {item.FoodName} x {item.Quantity}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1E293B") };
            var price = new Label { Text = item.PriceDisplay, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#4F46E5") };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(price, 1);
            row.Children.Add(name);
            row.Children.Add(price);
            itemsStack.Children.Add(row);
        }
        stack.Children.Add(itemsStack);

        // Special Instructions note
        if (!string.IsNullOrWhiteSpace(order.SpecialInstructions))
        {
            var noteBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#FEF3C7"),
                Stroke = Color.FromArgb("#FDE68A"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 6)
            };
            noteBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
            noteBorder.Content = new Label
            {
                Text = $"📝 Special Note: \"{order.SpecialInstructions}\"",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B45309")
            };
            stack.Children.Add(noteBorder);
        }

        // Status Explanation Note Box
        var noteBox = new Border
        {
            Padding = new Thickness(12, 8)
        };
        noteBox.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) };

        var noteText = new Label
        {
            Text = order.StatusNote,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.Center
        };

        if (order.Status == "Pending")
        {
            noteBox.BackgroundColor = Color.FromArgb("#FEF3C7");
            noteText.TextColor = Color.FromArgb("#B45309");
        }
        else if (order.Status == "Confirmed")
        {
            noteBox.BackgroundColor = Color.FromArgb("#EFF6FF");
            noteText.TextColor = Color.FromArgb("#1D4ED8");
        }
        else if (order.Status == "Delivered" || order.Status == "Completed")
        {
            noteBox.BackgroundColor = Color.FromArgb("#ECFDF5");
            noteText.TextColor = Color.FromArgb("#047857");
        }
        else if (order.Status == "Cancelled")
        {
            noteBox.BackgroundColor = Color.FromArgb("#FEF2F2");
            noteText.TextColor = Color.FromArgb("#DC2626");
        }

        noteBox.Content = noteText;
        stack.Children.Add(noteBox);

        border.Content = stack;
        return border;
    }

    private View BuildStepTracker(string status)
    {
        var outerBorder = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Stroke = Color.FromArgb("#E2E8F0"),
            StrokeThickness = 1,
            Padding = new Thickness(8, 8)
        };
        outerBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 4,
            VerticalOptions = LayoutOptions.Center
        };

        var b1 = new Border { StrokeThickness = 0, Padding = new Thickness(4, 6) };
        b1.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
        var l1 = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center };

        var b2 = new Border { StrokeThickness = 0, Padding = new Thickness(4, 6) };
        b2.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
        var l2 = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center };

        var b3 = new Border { StrokeThickness = 0, Padding = new Thickness(4, 6) };
        b3.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(8) };
        var l3 = new Label { FontSize = 11, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center };

        if (status == "Pending")
        {
            b1.BackgroundColor = Color.FromArgb("#F59E0B");
            l1.TextColor = Colors.White;
            l1.Text = "1. Placed ⏳";

            b2.BackgroundColor = Color.FromArgb("#F1F5F9");
            l2.TextColor = Color.FromArgb("#94A3B8");
            l2.Text = "2. Under Process 👨‍🍳";

            b3.BackgroundColor = Color.FromArgb("#F1F5F9");
            l3.TextColor = Color.FromArgb("#94A3B8");
            l3.Text = "3. Delivered 🚚";
        }
        else if (status == "Confirmed")
        {
            b1.BackgroundColor = Color.FromArgb("#10B981");
            l1.TextColor = Colors.White;
            l1.Text = "1. Placed ✔️";

            b2.BackgroundColor = Color.FromArgb("#2563EB");
            l2.TextColor = Colors.White;
            l2.Text = "2. Under Process 👨‍🍳";

            b3.BackgroundColor = Color.FromArgb("#F1F5F9");
            l3.TextColor = Color.FromArgb("#94A3B8");
            l3.Text = "3. Delivered 🚚";
        }
        else if (status == "Delivered" || status == "Completed")
        {
            b1.BackgroundColor = Color.FromArgb("#10B981");
            l1.TextColor = Colors.White;
            l1.Text = "1. Placed ✔️";

            b2.BackgroundColor = Color.FromArgb("#10B981");
            l2.TextColor = Colors.White;
            l2.Text = "2. Confirmed ✔️";

            b3.BackgroundColor = Color.FromArgb("#059669");
            l3.TextColor = Colors.White;
            l3.Text = "3. Delivered 🎉";
        }
        else if (status == "Cancelled")
        {
            b1.BackgroundColor = Color.FromArgb("#EF4444");
            l1.TextColor = Colors.White;
            l1.Text = "❌ Cancelled";

            b2.BackgroundColor = Color.FromArgb("#F1F5F9");
            l2.TextColor = Color.FromArgb("#94A3B8");
            l2.Text = "2. Confirmed";

            b3.BackgroundColor = Color.FromArgb("#F1F5F9");
            l3.TextColor = Color.FromArgb("#94A3B8");
            l3.Text = "3. Delivered";
        }

        b1.Content = l1;
        b2.Content = l2;
        b3.Content = l3;

        Grid.SetColumn(b1, 0);
        Grid.SetColumn(b2, 1);
        Grid.SetColumn(b3, 2);
        grid.Children.Add(b1);
        grid.Children.Add(b2);
        grid.Children.Add(b3);

        outerBorder.Content = grid;
        return outerBorder;
    }

    private View BuildPastOrderCard(OrderDto order)
    {
        var border = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = new Thickness(14, 12)
        };
        border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(14)
        };

        var stack = new VerticalStackLayout { Spacing = 8 };

        var hGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var badge = new Border
        {
            BackgroundColor = order.StatusColor,
            StrokeThickness = 0,
            Padding = new Thickness(10, 3),
            HorizontalOptions = LayoutOptions.Start
        };
        badge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) };
        badge.Content = new Label { Text = order.StatusEmoji, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };

        var total = new Label
        {
            Text = order.TotalDisplay,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A"),
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(badge, 0);
        Grid.SetColumn(total, 1);
        hGrid.Children.Add(badge);
        hGrid.Children.Add(total);
        stack.Children.Add(hGrid);

        // Items list
        foreach (var item in order.Items)
        {
            var iGrid = new Grid
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
            iGrid.Children.Add(fl);
            iGrid.Children.Add(pl);
            stack.Children.Add(iGrid);
        }

        stack.Children.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F1F5F9") });
        stack.Children.Add(new Label { Text = order.CreatedAtFormatted, FontSize = 11, TextColor = Color.FromArgb("#94A3B8") });

        border.Content = stack;
        return border;
    }
}