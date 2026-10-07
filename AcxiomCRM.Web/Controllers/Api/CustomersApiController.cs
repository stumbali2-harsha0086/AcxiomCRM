using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Web.Controllers.Api
{
    public class CustomerApiDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
    }

    public class CreateCustomerApiRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersApiController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? name, [FromQuery] string? email, [FromQuery] string? phone, [FromQuery] string? company)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var customers = await _customerService.GetCustomersAsync(user.Id, role, name, email, phone, company);

            var dtos = customers.Select(c => new CustomerApiDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate
            });

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var customer = await _customerService.GetCustomerByIdAsync(id, user.Id, role);
            if (customer == null) return NotFound(new { message = $"Customer with ID {id} not found or access denied." });

            var dto = new CustomerApiDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerApiRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.CustomerName)) return BadRequest(new { message = "Customer Name is required." });
            if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest(new { message = "Email address is required." });
            if (string.IsNullOrWhiteSpace(request.Phone)) return BadRequest(new { message = "Phone number is required." });

            var customer = new Customer
            {
                CustomerName = request.CustomerName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Status = "Active"
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _customerService.CreateCustomerAsync(customer, user.Id, ip);

            if (!result.Success)
            {
                if (result.Message.Contains("already exists")) return StatusCode(409, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            var dto = new CustomerApiDto
            {
                CustomerId = result.Customer!.CustomerId,
                CustomerCode = result.Customer.CustomerCode,
                CustomerName = result.Customer.CustomerName,
                Email = result.Customer.Email,
                Phone = result.Customer.Phone,
                CompanyName = result.Customer.CompanyName,
                Address = result.Customer.Address,
                City = result.Customer.City,
                State = result.Customer.State,
                Status = result.Customer.Status,
                CreatedDate = result.Customer.CreatedDate
            };

            return CreatedAtAction(nameof(GetCustomerById), new { id = dto.CustomerId }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] CreateCustomerApiRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var customer = new Customer
            {
                CustomerId = id,
                CustomerName = request.CustomerName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                Address = request.Address,
                City = request.City,
                State = request.State,
                Status = "Active"
            };

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _customerService.UpdateCustomerAsync(customer, user.Id, role, ip);

            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(new { message = result.Message });
                if (result.Message.Contains("Unauthorized")) return StatusCode(403, new { message = result.Message });
                if (result.Message.Contains("already exists")) return StatusCode(409, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Sales Executive";
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var result = await _customerService.DeleteCustomerAsync(id, user.Id, role, ip);

            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(new { message = result.Message });
                if (result.Message.Contains("Unauthorized")) return StatusCode(403, new { message = result.Message });
                return BadRequest(new { message = result.Message });
            }

            return Ok(new { message = result.Message });
        }
    }
}
