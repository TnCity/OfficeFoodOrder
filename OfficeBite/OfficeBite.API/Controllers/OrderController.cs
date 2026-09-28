using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeBite.BLL.Services;
using OfficeBite.Shared.DTOs;

namespace OfficeBite.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrderController(OrderService orderService)
    {
        _orderService = orderService;
    }


    // ==========================================
    // EMPLOYEE - PLACE ORDER
    // ==========================================

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        CreateOrderRequest request)
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

            var order =
                await _orderService.CreateOrderAsync(
                    userId,
                    request);

            return Ok(order);
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
    // EMPLOYEE - MY ORDERS
    // ==========================================

    [Authorize]
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
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

        var orders =
            await _orderService.GetMyOrdersAsync(
                userId);

        return Ok(orders);
    }


    // ==========================================
    // EMPLOYEE - SINGLE ORDER
    // ==========================================

    [Authorize]
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetMyOrder(
        int orderId)
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

        var order =
            await _orderService.GetMyOrderAsync(
                userId,
                orderId);

        if (order == null)
        {
            return NotFound(new
            {
                message = "Order not found."
            });
        }

        return Ok(order);
    }


    // ==========================================
    // ADMIN - ALL ORDERS
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/all")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders =
            await _orderService.GetAllOrdersAsync();

        return Ok(orders);
    }


    // ==========================================
    // ADMIN - TODAY'S ORDERS
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/today")]
    public async Task<IActionResult> GetTodayOrders()
    {
        var orders =
            await _orderService.GetTodayOrdersAsync();

        return Ok(orders);
    }


    // ==========================================
    // ADMIN - UPDATE STATUS
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPut("admin/{orderId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int orderId,
        string status)
    {
        try
        {
            var result =
                await _orderService.UpdateStatusAsync(
                    orderId,
                    status);

            if (!result)
            {
                return NotFound(new
                {
                    message = "Order not found."
                });
            }

            return Ok(new
            {
                message =
                    "Order status updated successfully."
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }


    }
    [Authorize(Roles = "Admin")]
    [HttpGet("admin/today-summary")]
    public async Task<IActionResult> GetTodaySummary()
    {
        var summary =
            await _orderService.GetTodaySummaryAsync();

        return Ok(summary);
    }

    // ==========================================
    // ADMIN - CREATE MANUAL ORDER FOR EMPLOYEE
    // ==========================================

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/manual")]
    public async Task<IActionResult> CreateManualOrder(
        AdminCreateOrderRequest request)
    {
        try
        {
            if (request.UserId <= 0)
            {
                return BadRequest(new
                {
                    message = "Please select an employee."
                });
            }

            var createRequest = new CreateOrderRequest
            {
                MenuId = request.MenuId,
                SpecialInstructions = request.SpecialInstructions,
                Items = request.Items
            };

            var order =
                await _orderService.CreateOrderAsync(
                    request.UserId,
                    createRequest);

            return Ok(order);
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
}