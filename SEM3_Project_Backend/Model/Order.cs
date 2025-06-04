namespace SEM3_Project_Backend.Model;

public enum PaymentStatus
{
    Pending, Cleared, Rejected
}

public enum DispatchStatus
{
    Pending, Dispatched, Cleared
}

public class Order
{
    //use simple numeric id, 8-digit order number is only used for frontend display
    public int Id { get; set; } 
    
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    public DateTime OrderDate { get; set; }
    
    //should be 01, 02, ... depends on the numbers of delivery type possible
    public int DeliveryTypeId { get; set; } 
    
    public PaymentStatus PaymentStatus { get; set; }
    public DispatchStatus DispatchStatus { get; set; }
    public DateTime DeliveryDate { get; set; }
    public float TotalAmount { get; set; }
}