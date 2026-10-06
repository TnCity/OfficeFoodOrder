using Microsoft.EntityFrameworkCore;
using OfficeBite.DAL.Data;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;
using OfficeBite.Shared.Helpers;

namespace OfficeBite.BLL.Services;

public class MenuService
{
    private readonly OfficeBiteDbContext _context;

    public MenuService(OfficeBiteDbContext context)
    {
        _context = context;
    }

    public async Task<MenuResponseDto> CreateMenuAsync(
        CreateMenuRequest request,
        int createdBy)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException(
                "Menu title is required.");

        if (request.Items == null ||
            request.Items.Count == 0)
        {
            throw new ArgumentException(
                "At least one food item is required.");
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.FoodName))
                throw new ArgumentException(
                    "Food name is required.");

            if (item.Price <= 0)
                throw new ArgumentException(
                    "Food price must be greater than zero.");
        }

        // India office date (IST)
        var menuDate = DateTimeHelper.TodayIst;

        var existingMenu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuDate == menuDate);

        if (existingMenu != null)
        {
            // If today's menu already exists, append new items to it
            foreach (var item in request.Items)
            {
                existingMenu.MenuItems.Add(new MenuItem
                {
                    FoodName = item.FoodName.Trim(),
                    Description = item.Description?.Trim(),
                    Price = item.Price,
                    IsAvailable = true
                });
            }

            await _context.SaveChangesAsync();
            return MapMenu(existingMenu);
        }

        var menu = new Menu
        {
            MenuDate = menuDate,
            Title = request.Title.Trim(),
            IsPublished = false,
            IsOrderingOpen = false,
            OrderStartTime = request.OrderStartTime,
            OrderEndTime = request.OrderEndTime,
            CreatedBy = createdBy,
            CreatedAt = DateTimeHelper.NowIst
        };

        foreach (var item in request.Items)
        {
            menu.MenuItems.Add(new MenuItem
            {
                FoodName = item.FoodName.Trim(),
                Description = item.Description?.Trim(),
                Price = item.Price,
                IsAvailable = true
            });
        }

        _context.Menus.Add(menu);

        await _context.SaveChangesAsync();

        return MapMenu(menu);
    }

    public static readonly (string FoodName, decimal Price)[] StandardDailyMenuItems = new[]
    {
        ("Fish Thali", 80m),
        ("Paneer Butter", 50m),
        ("Subin2paz", 40m),
        ("Vagturka", 35m),
        ("Chicken Ragla 2 Pes", 80m),
        ("Egg Cari", 20m),
        ("Plow", 60m),
        ("Aluporata + Chughni", 30m),
        ("Roti", 5m),
        ("Egg", 10m),
        ("Egg Turka", 45m),
        ("Tok Dahi", 10m),
        ("Bananna", 5m),
        ("Vagmills", 50m),
        ("Egg Thali", 60m),
        ("Khichuri", 40m),
        ("Chawmin Egg", 50m),
        ("Moglai", 170m),
        ("Piara", 15m)
    };

    public async Task<Menu> AutoSeedTodayMenuAsync()
    {
        var today = DateTimeHelper.TodayIst;

        var existing = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.MenuDate == today);

        if (existing != null)
        {
            // If existing menu has 0 items, seed the standard items
            if (!existing.MenuItems.Any())
            {
                foreach (var (foodName, price) in StandardDailyMenuItems)
                {
                    existing.MenuItems.Add(new MenuItem
                    {
                        FoodName = foodName,
                        Price = price,
                        IsAvailable = true
                    });
                }
                existing.IsPublished = true;
                existing.IsOrderingOpen = true;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        // Create standard daily menu
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
        var adminId = adminUser?.UserId ?? 1;

        var newMenu = new Menu
        {
            MenuDate = today,
            Title = $"Standard Lunch Menu - {today:dd MMM yyyy}",
            IsPublished = true,
            IsOrderingOpen = true,
            CreatedBy = adminId,
            CreatedAt = DateTimeHelper.NowIst
        };

        foreach (var (foodName, price) in StandardDailyMenuItems)
        {
            newMenu.MenuItems.Add(new MenuItem
            {
                FoodName = foodName,
                Price = price,
                IsAvailable = true
            });
        }

        _context.Menus.Add(newMenu);
        await _context.SaveChangesAsync();

        return newMenu;
    }

    public async Task<MenuResponseDto?> GetTodayMenuAsync()
    {
        var today = DateTimeHelper.TodayIst;

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuDate == today &&
                x.IsPublished);

        if (menu == null)
        {
            // Auto-seed today's standard menu so employees always have daily lunch menu available
            menu = await AutoSeedTodayMenuAsync();
        }

        if (menu == null)
            return null;

        if (!menu.IsOrderingOpen && menu.IsPublished)
        {
            menu.IsOrderingOpen = true;
            await _context.SaveChangesAsync();
        }

        var dto = new MenuResponseDto
        {
            MenuId = menu.MenuId,
            MenuDate = menu.MenuDate,
            Title = menu.Title,
            IsPublished = menu.IsPublished,
            IsOrderingOpen = true,
            OrderStartTime = menu.OrderStartTime,
            OrderEndTime = menu.OrderEndTime,
            Items = menu.MenuItems
                .Where(item => item.IsAvailable)
                .Select(item => new MenuItemResponseDto
                {
                    MenuItemId = item.MenuItemId,
                    FoodName = item.FoodName,
                    Description = item.Description,
                    Price = item.Price,
                    IsAvailable = item.IsAvailable
                })
                .ToList()
        };
        return dto;
    }

    public async Task<MenuResponseDto?> GetTodayAdminMenuAsync()
    {
        var today = DateTimeHelper.TodayIst;

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuDate == today);

        if (menu == null)
        {
            menu = await AutoSeedTodayMenuAsync();
        }

        if (menu == null)
            return null;

        if (!menu.IsOrderingOpen && menu.IsPublished)
        {
            menu.IsOrderingOpen = true;
            await _context.SaveChangesAsync();
        }

        // Admin sees ALL items (both Active and Inactive) so Admin can toggle and edit them
        return MapMenu(menu);
    }

    public async Task<MenuItemResponseDto> UpdateMenuItemAsync(int menuItemId, UpdateMenuItemRequest request)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null)
            throw new InvalidOperationException("Menu item not found.");

        if (string.IsNullOrWhiteSpace(request.FoodName))
            throw new ArgumentException("Food name is required.");

        if (request.Price <= 0)
            throw new ArgumentException("Price must be greater than zero.");

        item.FoodName = request.FoodName.Trim();
        item.Description = request.Description?.Trim();
        item.Price = request.Price;
        item.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();

        return new MenuItemResponseDto
        {
            MenuItemId  = item.MenuItemId,
            FoodName    = item.FoodName,
            Description = item.Description,
            Price       = item.Price,
            IsAvailable = item.IsAvailable
        };
    }

    public async Task<MenuItemResponseDto> ToggleMenuItemAvailabilityAsync(int menuItemId)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null)
            throw new InvalidOperationException("Menu item not found.");

        item.IsAvailable = !item.IsAvailable;
        await _context.SaveChangesAsync();

        return new MenuItemResponseDto
        {
            MenuItemId  = item.MenuItemId,
            FoodName    = item.FoodName,
            Description = item.Description,
            Price       = item.Price,
            IsAvailable = item.IsAvailable
        };
    }

    public async Task<bool> DeleteMenuItemAsync(int menuItemId)
    {
        var item = await _context.MenuItems.FirstOrDefaultAsync(i => i.MenuItemId == menuItemId);
        if (item == null)
            return false;

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<MenuResponseDto> UpdateMenuDetailsAsync(int menuId, string title, TimeOnly? orderStartTime, TimeOnly? orderEndTime)
    {
        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.MenuId == menuId);

        if (menu == null)
            throw new InvalidOperationException("Menu not found.");

        if (!string.IsNullOrWhiteSpace(title))
            menu.Title = title.Trim();

        menu.OrderStartTime = orderStartTime;
        menu.OrderEndTime = orderEndTime;

        await _context.SaveChangesAsync();
        return MapMenu(menu);
    }

    public async Task<bool> PublishMenuAsync(int menuId)
    {
        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuId == menuId);

        if (menu == null)
            return false;

        if (!menu.MenuItems.Any())
            throw new InvalidOperationException(
                "Menu must contain at least one food item.");

        menu.IsPublished = true;
        menu.IsOrderingOpen = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<MenuItemResponseDto>> AddMenuItemsAsync(
        int menuId,
        List<CreateMenuItemRequest> newItems)
    {
        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.MenuId == menuId);

        if (menu == null)
            throw new InvalidOperationException("Menu not found.");

        if (newItems == null || newItems.Count == 0)
            throw new ArgumentException("At least one item is required.");

        foreach (var item in newItems)
        {
            if (string.IsNullOrWhiteSpace(item.FoodName))
                throw new ArgumentException("Food name is required.");
            if (item.Price <= 0)
                throw new ArgumentException("Price must be greater than zero.");
        }

        var addedItems = new List<MenuItem>();
        foreach (var item in newItems)
        {
            var menuItem = new MenuItem
            {
                FoodName    = item.FoodName.Trim(),
                Description = item.Description?.Trim(),
                Price       = item.Price,
                IsAvailable = true
            };
            menu.MenuItems.Add(menuItem);
            addedItems.Add(menuItem);
        }

        await _context.SaveChangesAsync();

        return addedItems.Select(i => new MenuItemResponseDto
        {
            MenuItemId  = i.MenuItemId,
            FoodName    = i.FoodName,
            Description = i.Description,
            Price       = i.Price,
            IsAvailable = i.IsAvailable
        }).ToList();
    }

    public async Task<bool> CloseOrderingAsync(int menuId)
    {
        var menu = await _context.Menus
            .FirstOrDefaultAsync(x =>
                x.MenuId == menuId);

        if (menu == null)
            return false;

        menu.IsOrderingOpen = false;

        await _context.SaveChangesAsync();

        return true;
    }

    private static MenuResponseDto MapMenu(Menu menu)
    {
        return new MenuResponseDto
        {
            MenuId = menu.MenuId,
            MenuDate = menu.MenuDate,
            Title = menu.Title,
            IsPublished = menu.IsPublished,
            IsOrderingOpen = menu.IsOrderingOpen,
            OrderStartTime = menu.OrderStartTime,
            OrderEndTime = menu.OrderEndTime,

            Items = menu.MenuItems
                .Select(item => new MenuItemResponseDto
                {
                    MenuItemId = item.MenuItemId,
                    FoodName = item.FoodName,
                    Description = item.Description,
                    Price = item.Price,
                    IsAvailable = item.IsAvailable
                })
                .ToList()
        };
    }
}