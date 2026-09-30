using OfficeBite.Mobile.Models;
using OfficeBite.Mobile.Services;

namespace OfficeBite.Mobile.Views;

public partial class CreateMenuPage : ContentPage
{
    private readonly ApiService _apiService;
    private int? _existingMenuId = null;
    private bool _isPublished = false;
    private bool _isOrderingOpen = false;
    private List<MenuItemDto> _currentSavedItems = new();

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
                _currentSavedItems = menu.Items;

                ExistingMenuBanner.IsVisible = true;

                if (menu.IsPublished)
                {
                    BannerTitleLabel.Text = "🟢 Today's Menu is Live";
                    var activeCount = menu.Items.Count(i => i.IsAvailable);
                    ExistingMenuLabel.Text = $"{activeCount} of {menu.Items.Count} items active & visible to employees";
                    PublishButton.IsVisible = false;
                }
                else
                {
                    BannerTitleLabel.Text = "📝 Menu in Draft Mode";
                    ExistingMenuLabel.Text = $"{menu.Items.Count} items saved (tap Publish to make live)";
                    PublishButton.IsVisible = true;
                }

                RenderSavedItems(AdminSearchEntry.Text?.Trim() ?? string.Empty);
            }
            else
            {
                // Auto-seed standard menu
                await _apiService.ResetStandardMenuAsync();
                var seededMenu = await _apiService.GetAdminTodayMenuAsync();
                if (seededMenu != null)
                {
                    _existingMenuId = seededMenu.MenuId;
                    _isPublished    = seededMenu.IsPublished;
                    _isOrderingOpen = seededMenu.IsOrderingOpen;
                    _currentSavedItems = seededMenu.Items;
                    ExistingMenuBanner.IsVisible = true;
                    RenderSavedItems(AdminSearchEntry.Text?.Trim() ?? string.Empty);
                }
            }

            // Clear new item inputs
            FoodItemsStack.Children.Clear();
            _itemRows.Clear();
            UpdatePlaceholderVisibility();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load menu: {ex.Message}", "OK");
        }
    }

    private void AdminSearchEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderSavedItems(e.NewTextValue?.Trim() ?? string.Empty);
    }

    private void RenderSavedItems(string search = "")
    {
        SavedItemsStack.Children.Clear();

        if (_currentSavedItems.Count == 0)
        {
            SavedItemsSection.IsVisible = false;
            return;
        }

        SavedItemsSection.IsVisible = true;
        var activeCount = _currentSavedItems.Count(i => i.IsAvailable);
        ActiveCountSummaryLabel.Text = $"{activeCount} Active / {_currentSavedItems.Count} Total";
        SavedItemsHeader.Text = $"📋 Daily Menu Items ({_currentSavedItems.Count})";

        var filtered = string.IsNullOrWhiteSpace(search)
            ? _currentSavedItems
            : _currentSavedItems.Where(i => i.FoodName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        if (filtered.Count == 0)
        {
            var noMatchLabel = new Label
            {
                Text = $"No items match \"{search}\"",
                FontSize = 13,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10)
            };
            SavedItemsStack.Children.Add(noMatchLabel);
            return;
        }

        foreach (var item in filtered)
        {
            var card = CreateSavedItemCard(item);
            SavedItemsStack.Children.Add(card);
        }
    }

    private Border CreateSavedItemCard(MenuItemDto item)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8,
            Padding = new Thickness(12, 10)
        };

        // Left info: Food Name + Price + Status
        var infoStack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
        var nameLabel = new Label
        {
            Text = item.FoodName,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = item.IsAvailable ? Color.FromArgb("#0F172A") : Color.FromArgb("#64748B"),
            TextDecorations = item.IsAvailable ? TextDecorations.None : TextDecorations.Strikethrough
        };
        
        var subInfoStack = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var priceLabel = new Label
        {
            Text = $"Rs. {item.Price:0}",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = item.IsAvailable ? Color.FromArgb("#7C3AED") : Color.FromArgb("#94A3B8")
        };
        var statusBadge = new Label
        {
            Text = item.IsAvailable ? "• 🟢 Active (Available)" : "• ⚪ InActive (Not Available)",
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = item.IsAvailable ? Color.FromArgb("#16A34A") : Color.FromArgb("#DC2626")
        };
        subInfoStack.Children.Add(priceLabel);
        subInfoStack.Children.Add(statusBadge);

        infoStack.Children.Add(nameLabel);
        infoStack.Children.Add(subInfoStack);

        // Edit Button (for renaming / changing price)
        var editBtn = new Button
        {
            Text = "✏️ Edit",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = Color.FromArgb("#EEF2FF"),
            TextColor = Color.FromArgb("#4F46E5"),
            BorderColor = Color.FromArgb("#C7D2FE"),
            BorderWidth = 1,
            HeightRequest = 36,
            CornerRadius = 8,
            Padding = new Thickness(10, 0),
            VerticalOptions = LayoutOptions.Center
        };

        editBtn.Clicked += async (s, e) =>
        {
            var newName = await DisplayPromptAsync("Edit Food Name", "Enter item name:", initialValue: item.FoodName);
            if (newName == null) return; // User pressed Cancel
            var trimmedName = newName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName)) return;

            var newPriceStr = await DisplayPromptAsync("Edit Price", $"Enter price for '{trimmedName}':", initialValue: item.Price.ToString("0"), keyboard: Keyboard.Numeric);
            if (newPriceStr == null) return; // User pressed Cancel -> Silently exit without error!

            if (!decimal.TryParse(newPriceStr.Trim(), out var newPrice) || newPrice <= 0)
            {
                await DisplayAlert("Invalid Price", "Please enter a valid price greater than 0.", "OK");
                return;
            }

            // If nothing changed, exit silently without calling API
            if (trimmedName.Equals(item.FoodName, StringComparison.OrdinalIgnoreCase) && newPrice == item.Price)
            {
                return;
            }

            var (success, msg) = await _apiService.UpdateMenuItemAsync(item.MenuItemId, new UpdateMenuItemRequest
            {
                FoodName    = trimmedName,
                Price       = newPrice,
                IsAvailable = item.IsAvailable
            });

            if (success)
            {
                item.FoodName = trimmedName;
                item.Price    = newPrice;
                RenderSavedItems(AdminSearchEntry.Text?.Trim() ?? string.Empty);
            }
            else
            {
                await DisplayAlert("Update Notice", string.IsNullOrWhiteSpace(msg) ? "Could not update item." : msg, "OK");
            }
        };

        // InActive / Active Action Button
        // If Active: Button allows marking item "InActive" (Not Available for Today)
        // If InActive: Button allows marking item "Active" (Available for Today)
        var toggleBtn = new Button
        {
            Text = item.IsAvailable ? "🚫 InActive" : "✅ Active",
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = item.IsAvailable ? Color.FromArgb("#FEE2E2") : Color.FromArgb("#DCFCE7"),
            TextColor = item.IsAvailable ? Color.FromArgb("#DC2626") : Color.FromArgb("#15803D"),
            BorderColor = item.IsAvailable ? Color.FromArgb("#FCA5A5") : Color.FromArgb("#86EFAC"),
            BorderWidth = 1,
            HeightRequest = 36,
            CornerRadius = 8,
            Padding = new Thickness(10, 0),
            VerticalOptions = LayoutOptions.Center
        };

        toggleBtn.Clicked += async (s, e) =>
        {
            toggleBtn.IsEnabled = false;
            var (success, msg) = await _apiService.ToggleMenuItemAvailabilityAsync(item.MenuItemId);
            toggleBtn.IsEnabled = true;

            if (success)
            {
                item.IsAvailable = !item.IsAvailable;
                RenderSavedItems(AdminSearchEntry.Text?.Trim() ?? string.Empty);
            }
            else
            {
                await DisplayAlert("Error", msg, "OK");
            }
        };

        Grid.SetColumn(infoStack, 0);
        Grid.SetColumn(editBtn, 1);
        Grid.SetColumn(toggleBtn, 2);

        grid.Children.Add(infoStack);
        grid.Children.Add(editBtn);
        grid.Children.Add(toggleBtn);

        var card = new Border
        {
            BackgroundColor = item.IsAvailable ? Colors.White : Color.FromArgb("#F8FAFC"),
            Stroke = item.IsAvailable ? Color.FromArgb("#E2E8F0") : Color.FromArgb("#E2E8F0"),
            StrokeThickness = 1,
            Padding = new Thickness(0)
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
        card.Content = grid;

        return card;
    }

    private async void ResetStandardButton_Clicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Reset Standard Menu",
            "Do you want to reload/reset all 19 standard food items for today's menu?", "Yes, Reset", "Cancel");
        if (!confirm) return;

        var (success, msg) = await _apiService.ResetStandardMenuAsync();
        if (success)
        {
            await DisplayAlert("Standard Menu Loaded", "All 19 standard food items are ready for today!", "OK");
            await CheckExistingMenuAsync();
        }
        else
        {
            await DisplayAlert("Error", msg, "OK");
        }
    }

    private void AddItemButton_Clicked(object sender, EventArgs e) => AddItemRow();

    private void AddItemRow(string name = "", string price = "")
    {
        var nameEntry = new Entry
        {
            Placeholder      = "Food name (e.g. Biryani)",
            Text             = name,
            FontSize         = 14,
            HeightRequest    = 42,
            TextColor        = Color.FromArgb("#0F172A"),
            PlaceholderColor = Color.FromArgb("#94A3B8")
        };

        var priceEntry = new Entry
        {
            Placeholder      = "Price (Rs.)",
            Text             = price,
            Keyboard         = Keyboard.Numeric,
            FontSize         = 14,
            HeightRequest    = 42,
            TextColor        = Color.FromArgb("#0F172A"),
            PlaceholderColor = Color.FromArgb("#94A3B8")
        };

        var removeBtn = new Button
        {
            Text            = "✕",
            FontSize        = 13,
            FontAttributes  = FontAttributes.Bold,
            HeightRequest   = 32,
            WidthRequest    = 32,
            CornerRadius    = 16,
            BackgroundColor = Color.FromArgb("#FEE2E2"),
            TextColor       = Color.FromArgb("#DC2626"),
            Padding         = new Thickness(0)
        };

        var rowLabel = new Label
        {
            FontSize        = 12,
            FontAttributes  = FontAttributes.Bold,
            TextColor       = Color.FromArgb("#7C3AED"),
            VerticalOptions = LayoutOptions.Center
        };
        rowLabel.Text = $"New Item {_itemRows.Count + 1}";

        var headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetColumn(rowLabel, 0);
        Grid.SetColumn(removeBtn, 1);
        headerGrid.Children.Add(rowLabel);
        headerGrid.Children.Add(removeBtn);

        var fieldsGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = new GridLength(100) }
            },
            ColumnSpacing = 8
        };

        var nameBorder  = WrapEntry(nameEntry);
        var priceBorder = WrapEntry(priceEntry);

        Grid.SetColumn(nameBorder, 0);
        Grid.SetColumn(priceBorder, 1);
        fieldsGrid.Children.Add(nameBorder);
        fieldsGrid.Children.Add(priceBorder);

        var innerStack = new VerticalStackLayout { Spacing = 0 };
        innerStack.Children.Add(headerGrid);
        innerStack.Children.Add(fieldsGrid);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding         = new Thickness(12, 10)
        };
        card.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
        card.Content = innerStack;

        _itemRows.Add((nameEntry, priceEntry, card));

        removeBtn.Clicked += (s, e) =>
        {
            FoodItemsStack.Children.Remove(card);
            _itemRows.Remove((nameEntry, priceEntry, card));
            RefreshItemNumbers();
            UpdatePlaceholderVisibility();
        };

        FoodItemsStack.Children.Add(card);
        UpdatePlaceholderVisibility();
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
            CornerRadius = new CornerRadius(8)
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

    private void UpdatePlaceholderVisibility()
    {
        NoItemsPlaceholder.IsVisible = _itemRows.Count == 0;
        SaveFooterBorder.IsVisible   = _itemRows.Count > 0;
    }

    private async void SaveMenuButton_Clicked(object sender, EventArgs e)
    {
        if (_itemRows.Count == 0) return;

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
                var (success, message) = await _apiService.AddMenuItemsAsync(_existingMenuId.Value, items);
                if (success)
                {
                    await DisplayAlert("Items Added!", $"Added {items.Count} new item(s) to today's menu!", "OK");
                    await CheckExistingMenuAsync();
                }
                else
                {
                    await DisplayAlert("Error", message, "OK");
                }
            }
            else
            {
                var autoTitle = $"Menu - {DateTime.Now:dd MMM yyyy}";
                var (success, message) = await _apiService.CreateMenuAsync(
                    new CreateMenuRequest { Title = autoTitle, Items = items });

                if (success)
                {
                    await DisplayAlert("Saved!", "Menu saved! Items are now available for today.", "OK");
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