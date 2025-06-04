using Microsoft.EntityFrameworkCore;

namespace SEM3_Project_Backend.Data;

public class AppDbContext : DbContext
{
    //initialize db context
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}