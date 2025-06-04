namespace SEM3_Project_Backend.Models;

public enum Role
{
    Admin, 
    Employee, 
    User
}

public class User
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
    public Role Role { get; set; }
}