namespace SEM3_Project_Backend.Model;

public class Customer
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    //last created|modified time
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    
    public ICollection<Order>? Orders { get; set; }
    public ICollection<Feedback>? Feedbacks { get; set; }
}