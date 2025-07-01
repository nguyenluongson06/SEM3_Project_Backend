using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEM3_Project_Backend.Data;
using SEM3_Project_Backend.DTOs;
using SEM3_Project_Backend.Model;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context, IConfiguration config) : ControllerBase
{
    [HttpPost("register/customer")]
    [AllowAnonymous]
    public IActionResult RegisterCustomer([FromBody] RegisterRequest dto)
    {
        if (context.Customers.Any(c => c.Email == dto.Email))
            return BadRequest("Email already exists");

        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email,
            HashedPassword = HashPassword(dto.Password),
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        context.SaveChanges();
        return Ok();
    }

    //(admin only) register employee
    [HttpPost("register/employee")]
    [Authorize(Roles = "Admin")]
    public IActionResult RegisterEmployee([FromBody] RegisterRequest dto)
    {
        if (context.Employees.Any(e => e.Username == dto.Username))
            return BadRequest("Username already exists");

        var employee = new Employee
        {
            Username = dto.Username,
            HashedPassword = HashPassword(dto.Password),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);
        context.SaveChanges();
        return Ok();
    }

    // (admin only) register admin
    [HttpPost("register/admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult RegisterAdmin([FromBody] RegisterRequest dto)
    {
        if (context.Admins.Any(a => a.Username == dto.Username))
            return BadRequest("Username already exists");

        var admin = new Admin
        {
            Username = dto.Username,
            Password = HashPassword(dto.Password),
            Name = dto.Name,
            CreatedAt = DateTime.UtcNow
        };
        context.Admins.Add(admin);
        context.SaveChanges();
        return Ok();
    }

    // login for customer, employee, or admin
    // customer uses email as username
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest dto)
    {
        var admin = context.Admins.FirstOrDefault(a => a.Username == dto.Username);
        if (admin != null && VerifyPassword(dto.Password, admin.Password))
            return Ok(new { Token = GenerateJwtToken(admin.Username, "Admin"), Role = "Admin" });

        var emp = context.Employees.FirstOrDefault(e => e.Username == dto.Username);
        if (emp != null && VerifyPassword(dto.Password, emp.HashedPassword))
            return Ok(new { Token = GenerateJwtToken(emp.Username, "Employee"), Role = "Employee" });

        var cus = context.Customers.FirstOrDefault(c => c.Email == dto.Username);
        if (cus != null && !string.IsNullOrEmpty(cus.HashedPassword) && VerifyPassword(dto.Password, cus.HashedPassword))
        {
            var email = cus.Email ?? string.Empty;
            return Ok(new { Token = GenerateJwtToken(email, "Customer") });
        }

        return Unauthorized("Invalid credentials");
    }

    // change password for customer, employee, or admin
    [HttpPost("change-password")]
    [Authorize]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest dto)
    {
        var currentUsername = User.Identity?.Name;
        if (string.IsNullOrEmpty(currentUsername))
            return Unauthorized();

        // Check role from JWT
        if (User.IsInRole("Customer"))
        {
            var cus = context.Customers.FirstOrDefault(c => c.Email == currentUsername);
            if (cus != null && !string.IsNullOrEmpty(cus.HashedPassword) && VerifyPassword(dto.OldPassword, cus.HashedPassword))
            {
                cus.HashedPassword = HashPassword(dto.NewPassword);
                context.SaveChanges();
                return Ok();
            }
        }
        else if (User.IsInRole("Employee"))
        {
            var emp = context.Employees.FirstOrDefault(e => e.Username == currentUsername);
            if (emp != null && VerifyPassword(dto.OldPassword, emp.HashedPassword))
            {
                emp.HashedPassword = HashPassword(dto.NewPassword);
                context.SaveChanges();
                return Ok();
            }
        }
        else if (User.IsInRole("Admin"))
        {
            var admin = context.Admins.FirstOrDefault(a => a.Username == currentUsername);
            if (admin != null && VerifyPassword(dto.OldPassword, admin.Password))
            {
                admin.Password = HashPassword(dto.NewPassword);
                context.SaveChanges();
                return Ok();
            }
        }

        return BadRequest("Invalid credentials");
    }

    //(admin only) change employee password
    [HttpPost("admin/change-employee-password")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminChangeEmployeePassword([FromBody] ChangePasswordRequest dto)
    {
        var emp = context.Employees.FirstOrDefault(e => e.Username == dto.Username);
        if (emp == null) return NotFound();
        if (!VerifyPassword(dto.OldPassword, emp.HashedPassword))
            return BadRequest("Old password is incorrect");
        emp.HashedPassword = HashPassword(dto.NewPassword);
        context.SaveChanges();
        return Ok();
    }

    [HttpGet("customers")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetCustomers()
    {
        var customers = context.Customers.Select(c => new { c.Id, c.Name, c.Email }).ToList();
        return Ok(customers);
    }

    [HttpGet("employees")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetEmployees()
    {
        var employees = context.Employees.Select(e => new { e.Id, e.Username, e.Name }).ToList();
        return Ok(employees);
    }

    [HttpGet("admins")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmins()
    {
        var admins = context.Admins.Select(a => new { a.Id, a.Username, a.Name }).ToList();
        return Ok(admins);
    }

    [HttpDelete("customer/{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteCustomer(int id)
    {
        var customer = context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return NotFound();
        // Prevent deletion if referenced in orders, feedback, returns
        bool hasOrders = context.Orders.Any(o => o.CustomerId == id);
        bool hasFeedback = context.Feedbacks.Any(f => f.CustomerId == id);
        bool hasReturns = context.ReturnOrReplacements.Any(r => r.Order != null && r.Order.CustomerId == id);
        if (hasOrders || hasFeedback || hasReturns)
            return BadRequest("Cannot delete customer: referenced by orders, feedback, or returns.");
        context.Customers.Remove(customer);
        context.SaveChanges();
        return Ok();
    }

    [HttpDelete("employee/{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteEmployee(int id)
    {
        var emp = context.Employees.FirstOrDefault(e => e.Id == id);
        if (emp == null) return NotFound();
        // Prevent deletion if referenced in orders (if any future logic), or other entities
        // (Assume employees are not referenced elsewhere for now)
        context.Employees.Remove(emp);
        context.SaveChanges();
        return Ok();
    }

    [HttpDelete("admin/{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteAdmin(int id)
    {
        var admin = context.Admins.FirstOrDefault(a => a.Id == id);
        if (admin == null) return NotFound();
        // Prevent deletion of last admin
        if (context.Admins.Count() <= 1)
            return BadRequest("Cannot delete the last admin.");
        context.Admins.Remove(admin);
        context.SaveChanges();
        return Ok();
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hashed)
    {
        return HashPassword(password) == hashed;
    }

    private string GenerateJwtToken(string username, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}