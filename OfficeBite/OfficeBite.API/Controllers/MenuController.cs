using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly MenuService _menuService;

    public MenuController(MenuService menuService)
    {
        _menuService = menuService;
    }

    // ==========================================
    // EMPLOYEE - GET TODAY'S MENU
    // ==========================================

    [Authorize]
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayMenu()
    {
        var menu = await _menuService.GetTodayMenuAsync();

        if (menu == null)
        {
            return NotFound(new
            {
                message = "Today's menu is not available yet."
            });
        }

        return Ok(menu);
    }


    // ==========================================
    // ADMIN - CREATE TODAY'S MENU
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateMenu(
        CreateMenuRequest request)
    {
        try
        {
            var userIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                userIdClaim,
                out var userId))
            {
                return Unauthorized();
            }

            var menu =
                await _menuService.CreateMenuAsync(
                    request,
                    userId);

            return Ok(menu);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }


    // ==========================================
    // ADMIN - VIEW TODAY'S DRAFT/PUBLISHED MENU
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/today")]
    public async Task<IActionResult> GetTodayAdminMenu()
    {
        var menu =
            await _menuService.GetTodayAdminMenuAsync();

        if (menu == null)
        {
            return NotFound(new
            {
                message = "Today's menu has not been created."
            });
        }

        return Ok(menu);
    }


    // ==========================================
    // ADMIN - PUBLISH MENU
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("{menuId:int}/publish")]
    public async Task<IActionResult> PublishMenu(
        int menuId)
    {
        try
        {
            var result =
                await _menuService.PublishMenuAsync(
                    menuId);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Menu not found."
                });
            }

            return Ok(new
            {
                message =
                    "Today's menu has been published."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }


    // ==========================================
    // ADMIN - ADD ITEMS TO EXISTING MENU
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("{menuId:int}/items")]
    public async Task<IActionResult> AddMenuItems(
        int menuId,
        [FromBody] List<CreateMenuItemRequest> items)
    {
        try
        {
            var result = await _menuService.AddMenuItemsAsync(menuId, items);
            return Ok(new { message = "Items added successfully.", items = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }


    // ==========================================
    // ADMIN - UPDATE MENU ITEM (EDIT NAME / PRICE / AVAILABILITY)
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPut("items/{menuItemId:int}")]
    [HttpPost("items/{menuItemId:int}")]
    public async Task<IActionResult> UpdateMenuItem(
        int menuItemId,
        [FromBody] UpdateMenuItemRequest request)
    {
        try
        {
            var updated = await _menuService.UpdateMenuItemAsync(menuItemId, request);
            return Ok(new { message = "Item updated successfully.", item = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }


    // ==========================================
    // ADMIN - TOGGLE ITEM ACTIVE / INACTIVE
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPatch("items/{menuItemId:int}/toggle-availability")]
    [HttpPost("items/{menuItemId:int}/toggle-availability")]
    public async Task<IActionResult> ToggleMenuItemAvailability(
        int menuItemId)
    {
        try
        {
            var updated = await _menuService.ToggleMenuItemAvailabilityAsync(menuItemId);
            return Ok(new { message = $"Item is now {(updated.IsAvailable ? "Active" : "Inactive")}.", item = updated });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }


    // ==========================================
    // ADMIN - RE-SEED / ENSURE STANDARD MENU
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/reset-standard")]
    public async Task<IActionResult> ResetStandardMenu()
    {
        var menu = await _menuService.AutoSeedTodayMenuAsync();
        return Ok(new { message = "Standard daily menu loaded successfully.", menu });
    }


    // ==========================================
    // ADMIN - CLOSE ORDERING
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("{menuId:int}/close")]
    public async Task<IActionResult> CloseOrdering(
        int menuId)
    {
        var result =
            await _menuService.CloseOrderingAsync(
                menuId);

        if (!result)
        {
            return NotFound(new
            {
                message = "Menu not found."
            });
        }

        return Ok(new
        {
            message =
                "Today's food ordering is now closed."
        });
    }
}