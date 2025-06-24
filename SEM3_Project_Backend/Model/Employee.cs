namespace SEM3_Project_Backend.Model;

//only admins can create|modify|delete employees
public class Employee
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string HashedPassword { get; set; }
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
}