using Microsoft.EntityFrameworkCore;
using OfficeBite.DAL.Data;
using OfficeBite.DAL.Entities;
using OfficeBite.Shared.DTOs;

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

        // India office date
        var menuDate = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

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
            CreatedAt = DateTime.UtcNow
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

    public async Task<MenuResponseDto?> GetTodayMenuAsync()
    {
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuDate == today &&
                x.IsPublished);

        if (menu == null)
            return null;

        return MapMenu(menu);
    }

    public async Task<MenuResponseDto?> GetTodayAdminMenuAsync()
    {
        var today = DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(5.5));

        var menu = await _context.Menus
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x =>
                x.MenuDate == today);

        if (menu == null)
            return null;

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