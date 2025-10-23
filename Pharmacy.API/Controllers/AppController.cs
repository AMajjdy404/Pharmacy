using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.API.Errors;
using Pharmacy.Core.Interfaces;
using Pharmacy.Core.Models;
using Pharmacy.API.Dtos;
using Microsoft.AspNetCore.SignalR;
using Pharmacy.API.Hubs;

namespace Pharmacy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly IGenericRepository<DeliveryMan> _deliveryManRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AppController(IGenericRepository<DeliveryMan> deliveryManRepo,
            IGenericRepository<Order> orderRepo,
            IHubContext<NotificationHub> hubContext)
        {
            _deliveryManRepo = deliveryManRepo;
            _orderRepo = orderRepo;
            _hubContext = hubContext;
        }

        [HttpPut("updateAvailability")]
        [Authorize]
        public async Task<IActionResult> UpdateDeliveryManAvailability()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse(401));

            if (!int.TryParse(userIdStr, out var deliveryManId))
                return BadRequest(new ApiResponse(400, "Invalid user id in token"));

            var deliveryMan = await _deliveryManRepo.GetByIdAsync(deliveryManId);
            if (deliveryMan == null)
                return NotFound(new ApiResponse(404, "Delivery man not found"));

            // Toggle availability
            deliveryMan.IsAvaliable = !deliveryMan.IsAvaliable;

            _deliveryManRepo.Update(deliveryMan);
            await _deliveryManRepo.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync(
               "DeliveryManAvailabilityChanged",
               deliveryMan.Id,
               deliveryMan.IsAvaliable
             );

            return Ok(new ApiResponse(200, $"Delivery man availability updated to {deliveryMan.IsAvaliable}"));
        }

        [HttpGet("getPendingOrdersByDay")]
        [Authorize]
        public async Task<IActionResult> GetPendingOrdersByDay()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse(401));

            if (!int.TryParse(userIdStr, out var deliveryManId))
                return BadRequest(new ApiResponse(400, "Invalid user id in token"));

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var orders = await _orderRepo.GetAllAsync(
                o => o.DeliveryManId == deliveryManId &&
                     DateOnly.FromDateTime(o.OrderDate) == today &&
                o.OrderStatus == OrderStatus.Pending,
                o => o.Client // include Client
            );

            var ordersList = orders.ToList();

            if (!ordersList.Any())
                return NotFound(new ApiResponse(404, "No orders found for today"));

            return Ok(ordersList);
        }

        [HttpGet("getDeliveredOrdersByDay")]
        [Authorize]
        public async Task<IActionResult> GetDeliveredOrdersByDay()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse(401));

            if (!int.TryParse(userIdStr, out var deliveryManId))
                return BadRequest(new ApiResponse(400, "Invalid user id in token"));

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var orders = await _orderRepo.GetAllAsync(
                o => o.DeliveryManId == deliveryManId &&
                     DateOnly.FromDateTime(o.OrderDate) == today &&
                o.OrderStatus == OrderStatus.Delivered,
                o => o.Client // include Client
            );

            var ordersList = orders.ToList();

            if (!ordersList.Any())
                return NotFound(new ApiResponse(404, "No orders found for today"));

            return Ok(ordersList);
        }

        [HttpPut("setOrderInDelivery/{id}")]
        [Authorize]
        public async Task<IActionResult> SetOrderInDelivery(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            if (order.OrderStatus != OrderStatus.Pending)
                return BadRequest(new ApiResponse(400, "Order must be in Pending status to update to InDelivery"));

            order.OrderStatus = OrderStatus.InDelivery;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();

            // Notify admins group
            await _hubContext.Clients.Group("Admin").SendAsync("DeliveryManOrderStatusChanged", new
            {
                orderId = order.Id,
                status = order.OrderStatus.ToString(),
                clientName = order.Client?.Name ?? "Unknown",
                deliveryManId = order.DeliveryManId
            });

            return Ok(new { Message = "Order updated to InDelivery successfully" });
        }


        [HttpPut("setOrderDelivered/{id}")]
        [Authorize]
        public async Task<IActionResult> SetOrderDelivered(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            if (order.OrderStatus != OrderStatus.InDelivery)
                return BadRequest(new ApiResponse(400, "Order must be in InDelivery status to update to Delivered"));

            order.OrderStatus = OrderStatus.Delivered;
            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();

            // Notify admins group
            await _hubContext.Clients.Group("Admin").SendAsync("DeliveryManOrderStatusChanged", new
            {
                orderId = order.Id,
                status = order.OrderStatus.ToString(),
                clientName = order.Client?.Name ?? "Unknown",
                deliveryManId = order.DeliveryManId
            });

            return Ok(new { Message = "Order updated to Delivered successfully" });
        }

        [HttpGet("getProfile")]
        [Authorize]
        public async Task<ActionResult<DeliveryManDto>> getProfile()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse(401));

            if (!int.TryParse(userIdStr, out var deliveryManId))
                return BadRequest(new ApiResponse(400, "Invalid user id in token"));
            var deliveryMan = await _deliveryManRepo.GetByIdAsync(deliveryManId);
            if (deliveryMan is null)
                return NotFound(new ApiResponse(404, "Delivery Man is Not Found"));

            var result = new DeliveryManDto()
            {
                Id = deliveryMan.Id,
                Name = deliveryMan.Name,
                Email = deliveryMan.Email,
                IsAvaliable = deliveryMan.IsAvaliable,
            };
            return Ok(result);
        }

        [HttpGet("getAvailability")]
        [Authorize]
        public async Task<ActionResult> getAvailability()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new ApiResponse(401));

            if (!int.TryParse(userIdStr, out var deliveryManId))
                return BadRequest(new ApiResponse(400, "Invalid user id in token"));
            var deliveryMan = await _deliveryManRepo.GetByIdAsync(deliveryManId);
            if (deliveryMan is null)
                return NotFound(new ApiResponse(404, "Delivery Man is Not Found"));

            return Ok(new { Availability = deliveryMan.IsAvaliable });
        }

    }
}
