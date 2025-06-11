namespace SEM3_Project_Backend.Model;

public class Feedback
{
    public int Id { get; set; }
    
    //1-1 customer
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}