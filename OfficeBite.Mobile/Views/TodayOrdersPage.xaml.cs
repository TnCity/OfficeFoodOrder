using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class TodayOrdersPage : ContentPage
{
    private readonly ApiService _apiService;

    public TodayOrdersPage(ApiService apiService)
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
        OrdersStack.IsVisible      = false;
        NoOrdersBorder.IsVisible   = false;
        OrdersStack.Children.Clear();

        try
        {
            var orders = await _apiService.GetTodayOrdersAsync();
            if (orders.Count == 0) { NoOrdersBorder.IsVisible = true; return; }
            foreach (var order in orders) OrdersStack.Children.Add(BuildOrderCard(order));
            OrdersStack.IsVisible = true;
        }
        catch (Exception ex) { await DisplayAlert("Error", $"Could not load orders: {ex.Message}", "OK"); }
        finally
        {
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private View BuildOrderCard(OrderDto order)
    {
        var border = new Border { BackgroundColor = Colors.White, StrokeThickness = 0, Padding = new Thickness(16) };
        border.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) };

        var stack = new VerticalStackLayout { Spacing = 10 };

        // Header
        var hGrid = new Grid();
        hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        hGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nameLabel  = new Label { Text = order.UserName, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#111827"), VerticalOptions = LayoutOptions.Center };
        var totalLabel = new Label { Text = order.TotalDisplay, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#7C3AED"), VerticalOptions = LayoutOptions.Center };
        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(totalLabel, 1);
        hGrid.Children.Add(nameLabel);
        hGrid.Children.Add(totalLabel);
        stack.Children.Add(hGrid);

        // Status badge
        var badge = new Border { BackgroundColor = order.StatusColor, StrokeThickness = 0, Padding = new Thickness(10, 5), HorizontalOptions = LayoutOptions.Start };
        badge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(20) };
        badge.Content = new Label { Text = order.StatusEmoji, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
        stack.Children.Add(badge);

        // Items
        foreach (var item in order.Items)
        {
            var iGrid = new Grid();
            iGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            iGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var fl = new Label { Text = item.Display, FontSize = 14, TextColor = Color.FromArgb("#374151") };
            var pl = new Label { Text = item.PriceDisplay, FontSize = 14, TextColor = Color.FromArgb("#6B7280") };
            Grid.SetColumn(fl, 0); Grid.SetColumn(pl, 1);
            iGrid.Children.Add(fl); iGrid.Children.Add(pl);
            stack.Children.Add(iGrid);
        }

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

        stack.Children.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#E5E7EB") });

        // Action Buttons according to workflow: Pending -> Confirmed -> Delivered (or Cancelled)
        if (order.Status == "Pending")
        {
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
                if (success) await LoadOrdersAsync();
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
                if (success) await LoadOrdersAsync();
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
                if (success) await LoadOrdersAsync();
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
                if (success) await LoadOrdersAsync();
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
}