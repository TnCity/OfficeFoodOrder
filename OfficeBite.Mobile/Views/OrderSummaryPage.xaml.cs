using OfficeBite.Mobile.Helpers;
using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class OrderSummaryPage : ContentPage
{
    private readonly ApiService _apiService;
    private TodaySummaryDto? _summary;
    private List<OrderDto> _orders = new();
    private List<AggregatedFoodItem> _aggregatedFoodItems = new();
    private string? _lastGeneratedPdfPath;

    public OrderSummaryPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DateLabel.Text = DateTimeHelper.FormatIstFullDate();
        await LoadDataAsync();
    }

    private async void OnRefreshing(object sender, EventArgs e)
    {
        await LoadDataAsync();
        RefreshView.IsRefreshing = false;
    }

    private async Task LoadDataAsync()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            MainContentStack.IsVisible = false;

            var summaryTask = _apiService.GetTodaySummaryAsync();
            var ordersTask  = _apiService.GetTodayOrdersAsync();

            await Task.WhenAll(summaryTask, ordersTask);

            _summary = await summaryTask;
            _orders  = await ordersTask ?? new List<OrderDto>();

            ProcessData();
            UpdateUi();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load summary: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            MainContentStack.IsVisible = true;
        }
    }

    private void ProcessData()
    {
        // Calculate item-wise aggregate quantities from active orders
        var activeOrders = _orders.Where(o => o.Status != "Cancelled").ToList();

        var dict = new Dictionary<string, AggregatedFoodItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var order in activeOrders)
        {
            foreach (var item in order.Items)
            {
                var key = item.FoodName.Trim();
                if (!dict.TryGetValue(key, out var agg))
                {
                    agg = new AggregatedFoodItem
                    {
                        MenuItemId = item.MenuItemId,
                        FoodName = key,
                        UnitPrice = item.UnitPrice,
                        TotalQuantity = 0,
                        EmployeeList = new List<string>()
                    };
                    dict[key] = agg;
                }

                agg.TotalQuantity += item.Quantity;
                if (item.UnitPrice > 0 && agg.UnitPrice == 0)
                {
                    agg.UnitPrice = item.UnitPrice;
                }

                string empDisplay = $"{order.UserName} ({item.Quantity})";
                agg.EmployeeList.Add(empDisplay);
            }
        }

        foreach (var kvp in dict)
        {
            kvp.Value.EmployeeCount = kvp.Value.EmployeeList.Count;
            kvp.Value.EmployeeSummary = string.Join(", ", kvp.Value.EmployeeList);
        }

        _aggregatedFoodItems = dict.Values
            .OrderByDescending(x => x.TotalQuantity)
            .ThenBy(x => x.FoodName)
            .ToList();
    }

    private void UpdateUi()
    {
        int totalOrders = _summary?.TotalOrders ?? _orders.Count;
        int totalItemsCount = _aggregatedFoodItems.Sum(f => f.TotalQuantity);
        decimal totalAmount = _summary?.TotalAmount ?? _orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount);
        int pending = _summary?.PendingOrders ?? _orders.Count(o => o.Status == "Pending");
        int confirmed = _summary?.ConfirmedOrders ?? _orders.Count(o => o.Status == "Confirmed");
        int delivered = _summary?.DeliveredOrders ?? _orders.Count(o => o.Status == "Delivered" || o.Status == "Completed");

        TotalOrdersLabel.Text = totalOrders.ToString();
        TotalItemsLabel.Text  = totalItemsCount.ToString();
        TotalAmountLabel.Text = $"Rs.{totalAmount:0}";
        StatusBreakdownLabel.Text = $"{confirmed} Confirmed\n{pending} Pending | {delivered} Delivered";

        // Section 1: Food Items
        FoodItemsStack.Children.Clear();
        if (_aggregatedFoodItems.Count == 0)
        {
            NoFoodItemsBorder.IsVisible = true;
            FoodItemsCountLabel.Text = "0 items";
        }
        else
        {
            NoFoodItemsBorder.IsVisible = false;
            FoodItemsCountLabel.Text = $"{_aggregatedFoodItems.Count} item{(_aggregatedFoodItems.Count == 1 ? "" : "s")} ({totalItemsCount} qty)";

            foreach (var item in _aggregatedFoodItems)
            {
                var card = BuildFoodItemCard(item);
                FoodItemsStack.Children.Add(card);
            }
        }

        // Section 2: Employee Orders
        EmployeeOrdersStack.Children.Clear();
        if (_orders.Count == 0)
        {
            NoOrdersBorder.IsVisible = true;
            OrdersCountLabel.Text = "0 orders";
        }
        else
        {
            NoOrdersBorder.IsVisible = false;
            OrdersCountLabel.Text = $"{_orders.Count} order{(_orders.Count == 1 ? "" : "s")}";

            foreach (var order in _orders)
            {
                var card = BuildEmployeeOrderCard(order);
                EmployeeOrdersStack.Children.Add(card);
            }
        }
    }

    private View BuildFoodItemCard(AggregatedFoodItem item)
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

        var stack = new VerticalStackLayout { Spacing = 6 };

        // Row 1: Food Name & Total Quantity Badge
        var topGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameLabel = new Label
        {
            Text = $"🍽️ {item.FoodName}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A"),
            VerticalOptions = LayoutOptions.Center
        };

        var qtyBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#EDE9FE"),
            StrokeThickness = 0,
            Padding = new Thickness(12, 5),
            VerticalOptions = LayoutOptions.Center
        };
        qtyBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
        qtyBadge.Content = new Label
        {
            Text = $"x {item.TotalQuantity}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#6D28D9")
        };

        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(qtyBadge, 1);
        topGrid.Children.Add(nameLabel);
        topGrid.Children.Add(qtyBadge);
        stack.Children.Add(topGrid);

        // Row 2: Price Calculation if applicable
        if (item.UnitPrice > 0)
        {
            var priceLabel = new Label
            {
                Text = $"{item.PricePerUnitDisplay}  =  Total {item.TotalDisplay}",
                FontSize = 12,
                TextColor = Color.FromArgb("#64748B"),
                FontAttributes = FontAttributes.Bold
            };
            stack.Children.Add(priceLabel);
        }

        // Row 3: Employee List Breakdown
        if (!string.IsNullOrWhiteSpace(item.EmployeeSummary))
        {
            var empBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#F8FAFC"),
                Stroke = Color.FromArgb("#E2E8F0"),
                StrokeThickness = 1,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 4, 0, 0)
            };
            empBorder.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(8)
            };
            empBorder.Content = new Label
            {
                Text = $"👥 Ordered by: {item.EmployeeSummary}",
                FontSize = 12,
                TextColor = Color.FromArgb("#334155")
            };
            stack.Children.Add(empBorder);
        }

        border.Content = stack;
        return border;
    }

    private View BuildEmployeeOrderCard(OrderDto order)
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

        var stack = new VerticalStackLayout { Spacing = 6 };

        // Header: Employee Name + Status Badge + Total
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameStack = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        nameStack.Children.Add(new Label
        {
            Text = $"👤 {order.UserName}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#0F172A"),
            VerticalOptions = LayoutOptions.Center
        });

        var badge = new Border
        {
            BackgroundColor = order.StatusColor,
            StrokeThickness = 0,
            Padding = new Thickness(8, 2),
            VerticalOptions = LayoutOptions.Center
        };
        badge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(8)
        };
        badge.Content = new Label
        {
            Text = order.Status,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White
        };
        nameStack.Children.Add(badge);

        var totalLabel = new Label
        {
            Text = order.TotalDisplay,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#7C3AED"),
            VerticalOptions = LayoutOptions.Center
        };

        Grid.SetColumn(nameStack, 0);
        Grid.SetColumn(totalLabel, 1);
        headerGrid.Children.Add(nameStack);
        headerGrid.Children.Add(totalLabel);
        stack.Children.Add(headerGrid);

        // Items
        var itemsStr = string.Join(", ", order.Items.Select(i => $"{i.FoodName} x{i.Quantity}"));
        var itemsLabel = new Label
        {
            Text = $"• {itemsStr}",
            FontSize = 13,
            TextColor = Color.FromArgb("#475569")
        };
        stack.Children.Add(itemsLabel);

        if (!string.IsNullOrWhiteSpace(order.SpecialInstructions))
        {
            var noteLabel = new Label
            {
                Text = $"📝 Note: \"{order.SpecialInstructions}\"",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#B45309")
            };
            stack.Children.Add(noteLabel);
        }

        border.Content = stack;
        return border;
    }

    private async Task<string?> EnsurePdfGeneratedAsync()
    {
        try
        {
            string dateStr = DateTimeHelper.FormatIstFullDate();
            _lastGeneratedPdfPath = await PdfReportHelper.GenerateOrderSummaryPdfAsync(
                dateStr,
                _summary,
                _orders,
                _aggregatedFoodItems);

            return _lastGeneratedPdfPath;
        }
        catch (Exception ex)
        {
            await DisplayAlert("PDF Error", $"Failed to generate PDF: {ex.Message}", "OK");
            return null;
        }
    }

    private async void DownloadPdfButton_Clicked(object sender, EventArgs e)
    {
        var pdfPath = await EnsurePdfGeneratedAsync();
        if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath))
        {
            await DisplayAlert("Error", "Could not create PDF file.", "OK");
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Today's Order Summary PDF",
                File = new ReadOnlyFile(pdfPath)
            });
        }
        catch (Exception ex)
        {
            // If Launcher fails, offer to Share it
            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Today's Order Summary PDF",
                    File = new ShareFile(pdfPath)
                });
            }
            catch
            {
                await DisplayAlert("PDF Generated", $"PDF saved successfully at:\n{pdfPath}", "OK");
            }
        }
    }

    private async void SharePdfButton_Clicked(object sender, EventArgs e)
    {
        var pdfPath = await EnsurePdfGeneratedAsync();
        if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath))
        {
            await DisplayAlert("Error", "Could not create PDF file.", "OK");
            return;
        }

        try
        {
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "OfficeBite Food Order Summary PDF",
                File = new ShareFile(pdfPath)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Share Error", $"Could not share file: {ex.Message}", "OK");
        }
    }
}
