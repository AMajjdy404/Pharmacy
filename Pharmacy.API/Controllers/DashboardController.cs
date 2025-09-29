using System.Linq.Expressions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pharmacy.API.Dtos;
using Pharmacy.API.Errors;
using Pharmacy.Core.Interfaces;
using Pharmacy.Core.Models;
using Pharmacy.Core.Services;

namespace Pharmacy.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IGenericRepository<DeliveryMan> _deliveryManRepo;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGenericRepository<Client> _clientRepo;
        private readonly IPasswordHasher<DeliveryMan> _passwordHasher;
        private readonly IGenericRepository<Order> _orderRepo;

        public DashboardController(UserManager<AppUser> userManager,
            IGenericRepository<DeliveryMan> deliveryManRepo,
            RoleManager<IdentityRole> roleManager,
            IGenericRepository<Client> clientRepo,
            IPasswordHasher<DeliveryMan> passwordHasher,
            IGenericRepository<Order> orderRepo
            )
        {
            _userManager = userManager;
            _deliveryManRepo = deliveryManRepo;
            _roleManager = roleManager;
            _clientRepo = clientRepo;
            _passwordHasher = passwordHasher;
            _orderRepo = orderRepo;
        }


        #region AppUser
        [HttpPost("addAppUser")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAppUser(RegisterAppUserDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return BadRequest(new ApiResponse(400, "Email already exists"));

            var appUser = new AppUser
            {
                UserName = dto.Email.Split("@")[0],
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));


            if (!string.IsNullOrEmpty(dto.Role))
            {
                var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
                if (!roleExists)
                    return BadRequest(new ApiResponse(400, "Role is Not Exist"));

                await _userManager.AddToRoleAsync(appUser, dto.Role);
            }

            return Ok("AppUser registered successfully with role: " + dto.Role);
        }

        [HttpGet("getAllAppUsers")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAll()
        {
            var users = _userManager.Users
                .Select(u => new
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Email = u.Email
                })
                .ToList();

            return Ok(users);
        }



        [HttpGet("getAppUserById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse(404, "User not found"));

            var result = new
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(result);
        }


        [HttpPut("updateAppUser/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateAppUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse(404, "User not found"));

            user.Email = dto.Email ?? user.Email;

            if (!string.IsNullOrEmpty(dto.Password))
            {
                var removePass = await _userManager.RemovePasswordAsync(user);
                if (!removePass.Succeeded)
                    return BadRequest(removePass.Errors.Select(e => e.Description));

                var addPass = await _userManager.AddPasswordAsync(user, dto.Password);
                if (!addPass.Succeeded)
                    return BadRequest(addPass.Errors.Select(e => e.Description));
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));

            if (!string.IsNullOrEmpty(dto.Role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!await _roleManager.RoleExistsAsync(dto.Role))
                    return BadRequest(new ApiResponse(400, "Role does not exist"));

                await _userManager.AddToRoleAsync(user, dto.Role);
            }

            return Ok("User updated successfully");
        }


        [HttpDelete("deleteAppUser/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new ApiResponse(404, "User not found"));

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(new ApiResponse(400, result.Errors.Select(e => e.Description)));

            return Ok("User deleted successfully");
        }
        #endregion

        #region Client
        [HttpGet("getAllClients")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? name = null,
        [FromQuery] string? phoneNumber = null)
        {
            Expression<Func<Client, bool>> predicate = c =>
                (string.IsNullOrEmpty(name) || c.Name.Contains(name)) &&
                (string.IsNullOrEmpty(phoneNumber) || c.PhoneNumber.Contains(phoneNumber));

            var result = await _clientRepo.GetPagedAsync(
                page,
                pageSize,
                predicate,
                orderBy: c => c.Name,
                descending: false
            );

            // تحويل النتيجة لـ DTO
            var dtoResult = new PagedResult<ClientDto>
            {
                Items = result.Items.Select(c => new ClientDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address
                }).ToList(),
                TotalItems = result.TotalItems,
                Page = page,
                PageSize = pageSize
            };

            return Ok(dtoResult);
        }

        [HttpGet("getClientById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClientById(int id)
        {
            var client = await _clientRepo.GetByIdAsync(id);
            if (client == null)
                return NotFound(new ApiResponse(400, "Client not found"));
            var result = new ClientDto()
            {
                Id=id,
                Name = client.Name,
                PhoneNumber = client.PhoneNumber,
                Address = client.Address
            };

            return Ok(result);
        }

        [HttpPost("addClient")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddClient([FromBody] CreateClientDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var client = new Client
            {
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address
            };

            await _clientRepo.AddAsync(client);
            await _clientRepo.SaveChangesAsync();

            return Ok(new { message = "Client created successfully", client.Id });
        }

        [HttpPut("updateClient/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateClient(int id, [FromBody] UpdateClientDto dto)
        {
            var client = await _clientRepo.GetByIdAsync(id);
            if (client == null)
                return NotFound(new ApiResponse(400, "Client not found"));

            client.Name = dto.Name ?? client.Name;
            client.PhoneNumber = dto.PhoneNumber ?? client.PhoneNumber;
            client.Address = dto.Address ?? client.Address;

            _clientRepo.Update(client);
            await _clientRepo.SaveChangesAsync();

            return Ok("Client updated successfully");
        }

        [HttpDelete("deleteClient/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _clientRepo.GetByIdAsync(id);
            if (client == null)
                return NotFound(new ApiResponse(400, "Client not found"));

            _clientRepo.Delete(client);
            await _clientRepo.SaveChangesAsync();

            return Ok("Client deleted successfully");
        }

        #endregion

        #region Deliveryman
        [HttpPost("addDeliveryman")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDeliveryMan(RegisterDeliveryManDto dto)
        {
            if (await _deliveryManRepo.AnyAsync(d => d.Email == dto.Email))
                return BadRequest(new ApiResponse(400, "Email already exists"));

            var deliveryMan = new DeliveryMan
            {
                Name = dto.Name,
                Email = dto.Email
            };

            deliveryMan.Password = _passwordHasher.HashPassword(deliveryMan, dto.Password);

            await _deliveryManRepo.AddAsync(deliveryMan);
            await _deliveryManRepo.SaveChangesAsync();

            return Ok("DeliveryMan registered successfully");
        }

        [HttpGet("getAllDeliverymen")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDeliverymen([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? name = null, [FromQuery] string? email = null)
        {
            Expression<Func<DeliveryMan, bool>> predicate = d =>
                (string.IsNullOrEmpty(name) || d.Name.Contains(name)) &&
                (string.IsNullOrEmpty(email) || d.Email.Contains(email));

            var result = await _deliveryManRepo.GetPagedAsync(
                page,
                pageSize,
                predicate,
                orderBy: d => d.Name,
                descending: false
            );
            var response = new PagedResult<DeliveryMan>()
            {
                Items = result.Items,
                Page = page,
                PageSize = pageSize,
                TotalItems = result.TotalItems

            };

            return Ok(response);
        }

        [HttpGet("getDeliverymanById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeliverymanById(int id)
        {
            var deliveryMan = await _deliveryManRepo.GetByIdAsync(id);
            if (deliveryMan == null) return NotFound(new ApiResponse(404, "DeliveryMan not found"));

            return Ok(deliveryMan);
        }

        [HttpPut("updateDeliveryman/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] RegisterDeliveryManDto dto)
        {
            var deliveryMan = await _deliveryManRepo.GetByIdAsync(id);
            if (deliveryMan == null)
                return NotFound(new ApiResponse(404, "DeliveryMan not found"));

            deliveryMan.Name = dto.Name;
            deliveryMan.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                deliveryMan.Password = _passwordHasher.HashPassword(deliveryMan, dto.Password);
            }

            _deliveryManRepo.Update(deliveryMan);
            await _deliveryManRepo.SaveChangesAsync();

            return Ok("Delivery Man Updated Successfully");
        }

        [HttpDelete("deleteDeliveryman/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDeliveryman(int id)
        {
            var deliveryMan = await _deliveryManRepo.GetByIdAsync(id);
            if (deliveryMan == null) return NotFound(new ApiResponse(404, "DeliveryMan not found"));

            _deliveryManRepo.Delete(deliveryMan);
            await _deliveryManRepo.SaveChangesAsync();

            return Ok("DeliveryMan deleted successfully");
        }
        #endregion

        #region Order
        [HttpGet("getAllOrders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders(
        int page = 1,
        int pageSize = 10,
        string? clientName = null,
        DateOnly? orderDate = null)
        {
            Expression<Func<Order, bool>> filter = o =>
                (string.IsNullOrEmpty(clientName) || o.Client.Name.Contains(clientName)) &&
                (!orderDate.HasValue || DateOnly.FromDateTime(o.OrderDate) == orderDate.Value);

            var result = await _orderRepo.GetPagedAsync(
                page,
                pageSize,
                filter,
                o => o.OrderDate,
                descending: true,
                o => o.Client,
                o => o.DeliveryMan
            );

            if (result.Items.Count == 0)
                return NotFound(new ApiResponse(404, "No orders found"));
            var response = new PagedResult<OrderDto>()
            {
                Items = result.Items.Select(c => new OrderDto()
                {
                    Id = c.Id,
                    Address = c.Address,
                    Amount = c.Amount,
                    Client = c.Client.Name,
                    DeliveryMan = c.DeliveryMan.Name,
                    Distance = c.Distance,
                    OrderStatus = c.OrderStatus.ToString(),
                    OrderDate = c.OrderDate
                }).ToList(),
                TotalItems = result.TotalItems,
                Page = page,
                PageSize = pageSize,
            };

            return Ok(response);
        }

        [HttpGet("getOrderById/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderRepo.GetFirstOrDefaultAsync(
                o => o.Id == id,
                o => o.Client,
                o => o.DeliveryMan
            );

            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));
            var response = new OrderDto()
            {
                Id= order.Id,
                Address = order.Address,
                Amount = order.Amount,
                Client = order.Client.Name,
                DeliveryMan = order.DeliveryMan.Name,
                Distance = order.Distance,
                OrderStatus = order.OrderStatus.ToString(),
                OrderDate = order.OrderDate
            };

            return Ok(response);
        }

        [HttpPost("addOrder")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOrder([FromBody] CreateOrderDto dto)
        {
            if (dto == null)
                return BadRequest(new ApiResponse(400, "Invalid order data"));

            var order = new Order
            {
                Amount = dto.Amount,
                Address = dto.Address,
                DeliveryManId = dto.DeliveryManId,
                ClientId = dto.ClientId,
                OrderStatus = OrderStatus.Pending
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveChangesAsync();

            return Ok(new ApiResponse(200, "Order Created successfully"));
        }

        [HttpPut("updateOrder/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            order.Amount = dto.Amount;
            order.Address = dto.Address;
            order.ClientId = dto.ClientId;
            order.DeliveryManId = dto.DeliveryManId;

            _orderRepo.Update(order);
            await _orderRepo.SaveChangesAsync();

            return Ok("Order Updated Successfully");
        }

        [HttpDelete("deleteOrder/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _orderRepo.GetByIdAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            _orderRepo.Delete(order);
            await _orderRepo.SaveChangesAsync();

            return Ok(new ApiResponse(200, "Order deleted successfully"));
        }

        [HttpGet("getAvailableDeliveryMen")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAvailableDeliveryMen()
        {
            var availableDeliveryMen = await _deliveryManRepo.GetAllAsync(
                d => d.IsAvaliable == true
            );

            var list = availableDeliveryMen.Select(d => new DeliveryMenDto()
            {
                Id = d.Id,
                Name = d.Name
            }).ToList();

            if (!list.Any())
                return NotFound(new ApiResponse(404, "No available delivery men found"));

            return Ok(list);
        }

        [HttpGet("getClientsNameAndAddress")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClientsNameAndAddress()
        {
            var clients = await _clientRepo.GetAllAsync();

            var result = clients.Select(c => new ClientNameAddressDto
            {
                Name = c.Name,
                Address = c.Address
            }).ToList();

            if (!result.Any())
                return NotFound(new ApiResponse(404, "No clients found"));

            return Ok(result);
        }


        #endregion

        [HttpGet("getInDeliveryOrders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetInDeliveryOrders()
        {
            var orders = await _orderRepo.GetAllAsync(
                o => o.OrderStatus == OrderStatus.InDelivery,
                o => o.Client,
                o => o.DeliveryMan
            );

            var result = orders.Select(o => new InDeliveryOrderDto
            {
                OrderId = o.Id,
                ClientName = o.Client.Name,
                DeliveryManName = o.DeliveryMan.Name,
                Amount = o.Amount,
                Address = o.Address
            }).ToList();

            if (!result.Any())
                return NotFound(new ApiResponse(404, "No orders with status InDelivery found"));

            return Ok(result);
        }

    }
}
