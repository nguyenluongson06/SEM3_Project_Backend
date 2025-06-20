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
            Role = dto.Role ?? "Employee",
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);
        context.SaveChanges();
        return Ok();
    }

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

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest dto)
    {
        var admin = context.Admins.FirstOrDefault(a => a.Username == dto.Username);
        if (admin != null && VerifyPassword(dto.Password, admin.Password))
            return Ok(new { Token = GenerateJwtToken(admin.Username, "Admin") });

        var emp = context.Employees.FirstOrDefault(e => e.Username == dto.Username);
        if (emp != null && VerifyPassword(dto.Password, emp.HashedPassword))
            return Ok(new { Token = GenerateJwtToken(emp.Username, emp.Role ?? "Employee") });

        var cus = context.Customers.FirstOrDefault(c => c.Email == dto.Username);
        if (cus != null && VerifyPassword(dto.Password, cus.HashedPassword))
            return Ok(new { Token = GenerateJwtToken(cus.Email, "Customer") });

        return Unauthorized("Invalid credentials");
    }

    [HttpPost("change-password")]
    [Authorize]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest dto)
    {
        var cus = context.Customers.FirstOrDefault(c => c.Email == dto.Username);
        if (cus != null && VerifyPassword(dto.OldPassword, cus.HashedPassword))
        {
            cus.HashedPassword = HashPassword(dto.NewPassword);
            context.SaveChanges();
            return Ok();
        }

        var emp = context.Employees.FirstOrDefault(e => e.Username == dto.Username);
        if (emp != null && VerifyPassword(dto.OldPassword, emp.HashedPassword))
        {
            emp.HashedPassword = HashPassword(dto.NewPassword);
            context.SaveChanges();
            return Ok();
        }

        return BadRequest("Invalid username or password");
    }

    [HttpPost("admin/change-employee-password")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminChangeEmployeePassword([FromBody] RegisterRequest dto)
    {
        var emp = context.Employees.FirstOrDefault(e => e.Username == dto.Username);
        if (emp == null) return NotFound();
        emp.HashedPassword = HashPassword(dto.Password);
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
        var employees = context.Employees.Select(e => new { e.Id, e.Username, e.Name, e.Role }).ToList();
        return Ok(employees);
    }

    [HttpGet("admins")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmins()
    {
        var admins = context.Admins.Select(a => new { a.Id, a.Username, a.Name }).ToList();
        return Ok(admins);
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