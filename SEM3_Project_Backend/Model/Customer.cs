namespace SEM3_Project_Backend.Model;

public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime RegistrationDate { get; set; }
    
    public ICollection<Order>? Orders { get; set; }
    public ICollection<Feedback>? Feedbacks { get; set; }
}