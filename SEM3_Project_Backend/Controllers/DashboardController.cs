using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SEM3_Project_Backend.Data;
using System.Globalization;
using SEM3_Project_Backend.Model; // Add this if PaymentStatus is in Models namespace

namespace SEM3_Project_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "EmployeeOrAdmin")]
public class DashboardController(AppDbContext context) : ControllerBase
{
    // GET /api/dashboard/summary?from=2025-06-01&to=2025-06-30
    [HttpGet("summary")]
    public IActionResult GetSummary([FromQuery] string? range)
    {
        int days = 30;
        if (!string.IsNullOrEmpty(range) && range.EndsWith("d") && int.TryParse(range.TrimEnd('d'), out var d))
            days = d;

        var end = DateTime.UtcNow;
        var start = end.AddDays(-days + 1);

        var orders = context.Orders
            .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.PaymentStatus == PaymentStatus.Cleared);

        var totalRevenue = orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;
        var orderCount = orders.Count();
        var newCustomers = context.Customers.Count(c => c.CreatedAt >= start && c.CreatedAt <= end);

        return Ok(new
        {
            totalRevenue,
            orderCount,
            newCustomers
        });
    }

    // GET /api/dashboard/revenue-trend?range=7d
    [HttpGet("revenue-trend")]
    public IActionResult GetRevenueTrend([FromQuery] string? range)
    {
        int days = 7;
        if (!string.IsNullOrEmpty(range) && range.EndsWith("d") && int.TryParse(range.TrimEnd('d'), out var d))
            days = d;

        var end = DateTime.UtcNow.Date;
        var start = end.AddDays(-days + 1);

        var orders = context.Orders
            .Where(o => o.CreatedAt >= start && o.CreatedAt <= end && o.PaymentStatus == PaymentStatus.Cleared)
            .ToList();

        var trend = Enumerable.Range(0, days)
            .Select(i =>
            {
                var date = start.AddDays(i);
                var revenue = orders
                    .Where(o => o.CreatedAt.Date == date)
                    .Sum(o => (decimal?)o.TotalAmount) ?? 0;
                return new
                {
                    date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    revenue
                };
            })
            .ToList();

        return Ok(trend);
    }

    // GET /api/dashboard/top-products?limit=5
    [HttpGet("top-products")]
    public IActionResult GetTopProducts([FromQuery] int limit = 5)
    {
        var topProducts = context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.Product != null)
            .GroupBy(oi => new { oi.ProductId, Name = oi.Product.Name })
            .Select(g => new
            {
                productName = g.Key.Name ?? g.Key.ProductId.ToString(),
                unitsSold = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.unitsSold)
            .Take(limit)
            .ToList();

        return Ok(topProducts);
    }

    // GET /api/dashboard/low-stock?threshold=5
    [HttpGet("low-stock")]
    public IActionResult GetLowStock([FromQuery] int threshold = 5)
    {
        var lowStock = context.InventoryItems
            .Include(i => i.Product)
            .Where(i => i.Quantity <= threshold)
            .Select(i => new
            {
                productName = i.Product != null ? i.Product.Name : i.ProductId,
                stock = i.Quantity
            })
            .OrderBy(i => i.stock)
            .ToList();

        return Ok(lowStock);
    }
}