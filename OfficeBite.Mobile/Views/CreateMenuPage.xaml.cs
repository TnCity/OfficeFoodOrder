using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class CreateMenuPage : ContentPage
{
    private readonly ApiService _apiService;
    private int?  _existingMenuId = null;
    private bool  _isPublished    = false;
    private bool  _isOrderingOpen = false;

    // Tracks dynamically added input rows (NameEntry, PriceEntry, Card)
    private readonly List<(Entry NameEntry, Entry PriceEntry, Border Card)> _itemRows = new();

    public CreateMenuPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void BackButton_Clicked(object sender, EventArgs e) =>
        await Navigation.PopAsync();

    protected override bool OnBackButtonPressed()
    {
        Navigation.PopAsync();
        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckExistingMenuAsync();
    }

    private async Task CheckExistingMenuAsync()
    {
        try
        {
            var menu = await _apiService.GetAdminTodayMenuAsync();

            if (menu != null)
            {
                _existingMenuId = menu.MenuId;
                _isPublished    = menu.IsPublished;
                _isOrderingOpen = menu.IsOrderingOpen;

                ExistingMenuBanner.IsVisible = true;

                if (menu.IsPublished)
                {
                    BannerTitleLabel.Text = "🟢 Menu is Live & Published";
                    ExistingMenuLabel.Text = $"{menu.Items.Count} item(s) visible to employees";
                    PublishButton.IsVisible = false;
                }
                else
                {
                    BannerTitleLabel.Text = "📝 Menu in Draft Mode";
                    ExistingMenuLabel.Text = $"{menu.Items.Count} item(s) saved (not published yet)";
                    PublishButton.IsVisible = true;
                }

                // Render saved items
                SavedItemsStack.Children.Clear();
                if (menu.Items.Count > 0)
                {
                    SavedItemsSection.IsVisible = true;
                    SavedItemsHeader.Text = $"📋 Current Menu Items ({menu.Items.Count})";

                    foreach (var item in menu.Items)
                    {
                        var itemCard = CreateSavedItemCard(item);
                        SavedItemsStack.Children.Add(itemCard);
                    }
                }
                else
                {
                    SavedItemsSection.IsVisible = false;
                }

                AddItemsHeaderLabel.Text = "➕ Add More Food Items";
                SaveMenuButton.Text = "💾 Save Items to Menu";
                SaveMenuButton.IsEnabled = true;
                SaveMenuButton.BackgroundColor = Color.FromArgb("#7C3AED");

                // Clear new item inputs
                FoodItemsStack.Children.Clear();
                _itemRows.Clear();
                UpdatePlaceholderVisibility();
            }
            else
            {
                _existingMenuId = null;
                _isPublished    = false;
                _isOrderingOpen = false;

                ExistingMenuBanner.IsVisible  = false;
                SavedItemsSection.IsVisible   = false;

                AddItemsHeaderLabel.Text = "🍱 Add Food Items";
                SaveMenuButton.Text = "💾 Save Menu";
                SaveMenuButton.IsEnabled = true;
                SaveMenuButton.BackgroundColor = Color.FromArgb("#7C3AED");

                FoodItemsStack.Children.Clear();
                _itemRows.Clear();
                AddItemRow();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load menu: {ex.Message}", "OK");
        }
    }

    private static Border CreateSavedItemCard(MenuItemDto item)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };

        var nameLabel = new Label
        {
            Text = $"•  {item.FoodName}",
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1E293B"),
            VerticalOptions = LayoutOptions.Center
        };

        var priceBadge = new Border
        {
            BackgroundColor = Color.FromArgb("#F0FDF4"),
            Stroke = Color.FromArgb("#BBF7D0"),
            StrokeThickness = 1,
            Padding = new Thickness(10, 4)
        };
        priceBadge.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
        priceBadge.Content = new Label
        {
            Text = $"Rs. {item.Price:0}",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#15803D")
        };

        Grid.SetColumn(nameLabel, 0);
        Grid.SetColumn(priceBadge, 1);
        grid.Children.Add(nameLabel);
        grid.Children.Add(priceBadge);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = new Thickness(14, 10)
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(10)
        };
        card.Content = grid;

        return card;
    }

    private void AddItemButton_Clicked(object sender, EventArgs e) => AddItemRow();

    private void AddItemRow(string name = "", string price = "")
    {
        // Name entry
        var nameEntry = new Entry
        {
            Placeholder      = "Food name  (e.g. Biryani)",
            Text             = name,
            FontSize         = 15,
            HeightRequest    = 46,
            TextColor        = Color.FromArgb("#0F172A"),
            PlaceholderColor = Color.FromArgb("#94A3B8")
        };

        // Price entry
        var priceEntry = new Entry
        {
            Placeholder      = "Price (Rs.)",
            Text             = price,
            Keyboard         = Keyboard.Numeric,
            FontSize         = 15,
            HeightRequest    = 46,
            TextColor        = Color.FromArgb("#0F172A"),
            PlaceholderColor = Color.FromArgb("#94A3B8")
        };

        // Remove button
        var removeBtn = new Button
        {
            Text            = "✕",
            FontSize        = 14,
            FontAttributes  = FontAttributes.Bold,
            HeightRequest   = 36,
            WidthRequest    = 36,
            CornerRadius    = 18,
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            TextColor       = Color.FromArgb("#DC2626"),
            Padding         = new Thickness(0)
        };

        // Row number label
        var rowLabel = new Label
        {
            FontSize        = 12,
            FontAttributes  = FontAttributes.Bold,
            TextColor       = Color.FromArgb("#7C3AED"),
            VerticalOptions = LayoutOptions.Center
        };
        rowLabel.Text = $"New Item {_itemRows.Count + 1}";

        // Header: item label + remove button
        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Margin = new Thickness(0, 0, 0, 6)
        };
        Grid.SetColumn(rowLabel, 0);
        Grid.SetColumn(removeBtn, 1);
        headerGrid.Children.Add(rowLabel);
        headerGrid.Children.Add(removeBtn);

        // Name + Price side by side
        var fieldsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(110) }
            },
            ColumnSpacing = 10
        };

        var nameBorder  = WrapEntry(nameEntry);
        var priceBorder = WrapEntry(priceEntry);

        Grid.SetColumn(nameBorder, 0);
        Grid.SetColumn(priceBorder, 1);
        fieldsGrid.Children.Add(nameBorder);
        fieldsGrid.Children.Add(priceBorder);

        // Card container
        var innerStack = new VerticalStackLayout { Spacing = 0 };
        innerStack.Children.Add(headerGrid);
        innerStack.Children.Add(fieldsGrid);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding         = new Thickness(14, 12)
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(14)
        };
        card.Content = innerStack;

        _itemRows.Add((nameEntry, priceEntry, card));

        // Remove handler
        removeBtn.Clicked += (s, e) =>
        {
            FoodItemsStack.Children.Remove(card);
            _itemRows.Remove((nameEntry, priceEntry, card));
            RefreshItemNumbers();
            UpdatePlaceholderVisibility();
        };

        FoodItemsStack.Children.Add(card);
        NoItemsPlaceholder.IsVisible = false;
    }

    private static Border WrapEntry(Entry entry)
    {
        var b = new Border
        {
            BackgroundColor = Color.FromArgb("#F8FAFC"),
            Stroke          = Color.FromArgb("#E2E8F0"),
            StrokeThickness = 1,
            Padding         = new Thickness(10, 0)
        };
        b.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(10)
        };
        b.Content = entry;
        return b;
    }

    private void RefreshItemNumbers()
    {
        int idx = 1;
        foreach (var child in FoodItemsStack.Children)
        {
            if (child is Border card &&
                card.Content is VerticalStackLayout vsl &&
                vsl.Children[0] is Grid hdr &&
                hdr.Children[0] is Label lbl)
            {
                lbl.Text = $"New Item {idx++}";
            }
        }
    }

    private void UpdatePlaceholderVisibility() =>
        NoItemsPlaceholder.IsVisible = _itemRows.Count == 0;

    private async void SaveMenuButton_Clicked(object sender, EventArgs e)
    {
        if (_itemRows.Count == 0)
        {
            if (_existingMenuId != null)
            {
                await DisplayAlert("Add Items", "Tap '+ Add Item' above to enter new food items.", "OK");
            }
            else
            {
                await DisplayAlert("Missing", "Please add at least one food item.", "OK");
            }
            return;
        }

        var items = new List<CreateMenuItemRequest>();
        foreach (var (nameE, priceE, _) in _itemRows)
        {
            var foodName = nameE.Text?.Trim();
            if (string.IsNullOrWhiteSpace(foodName))
            {
                await DisplayAlert("Missing", "Please fill in all food names.", "OK");
                return;
            }
            if (!decimal.TryParse(priceE.Text?.Trim(), out var price) || price <= 0)
            {
                await DisplayAlert("Invalid Price", $"Enter a valid price for '{foodName}'.", "OK");
                return;
            }
            items.Add(new CreateMenuItemRequest
            {
                FoodName = foodName,
                Price    = price
            });
        }

        SaveMenuButton.IsEnabled   = false;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;

        try
        {
            if (_existingMenuId != null)
            {
                // Append items to existing menu (works whether published or draft)
                var (success, message) = await _apiService.AddMenuItemsAsync(_existingMenuId.Value, items);

                if (success)
                {
                    await DisplayAlert("Items Added!", $"Added {items.Count} item(s) to today's menu!", "OK");
                    await CheckExistingMenuAsync();
                }
                else
                {
                    await DisplayAlert("Error", message, "OK");
                }
            }
            else
            {
                // Create brand new menu
                var autoTitle = $"Menu - {DateTime.Now:dd MMM yyyy}";
                var (success, message) = await _apiService.CreateMenuAsync(
                    new CreateMenuRequest { Title = autoTitle, Items = items });

                if (success)
                {
                    await DisplayAlert("Saved!", "Menu saved! Tap 'Publish' to make it visible to employees.", "OK");
                    await CheckExistingMenuAsync();
                }
                else
                {
                    await DisplayAlert("Error", message, "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Connection failed: {ex.Message}", "OK");
        }
        finally
        {
            SaveMenuButton.IsEnabled   = true;
            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }
    }

    private async void PublishButton_Clicked(object sender, EventArgs e)
    {
        if (_existingMenuId == null) return;
        bool confirm = await DisplayAlert("Publish Menu",
            "Publish this menu? Employees will be able to see and order immediately.", "Publish", "Cancel");
        if (!confirm) return;

        var (success, message) = await _apiService.PublishMenuAsync(_existingMenuId.Value);
        if (success)
        {
            await DisplayAlert("Published!", "Today's menu is now live for employees!", "OK");
            await CheckExistingMenuAsync();
        }
        else
        {
            await DisplayAlert("Error", message, "OK");
        }
    }
}