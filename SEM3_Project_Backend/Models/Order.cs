namespace SEM3_Project_Backend.Models;

public enum OrderStatus
{
    Pending, //before user paid
    Accepted, //after user paid
    InTransit, //can only be changed to by admin|employee
    Delivered
}


public class Order
{
    public int Id { get; set; }
    
    //many-1 with user
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public DateTime OrderDate { get; set; }
    public float TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
}