using System.Text;

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
    
    //should be 01, 02, ... depends on the numbers of delivery type possible; could change to enum
    public int DeliveryTypeId { get; set; } 
    
    public PaymentStatus PaymentStatus { get; set; }
    //should be range?
    public DispatchStatus DispatchStatus { get; set; }
    public DateTime DeliveryDate { get; set; }
    public float TotalAmount { get; set; }
    
    //list of items in order
    public ICollection<OrderItem>? OrderItems { get; set; }
    
    //linked payment
    public Payment Payment { get; set; }
    
    //returns|replacements
    public ICollection<ReturnOrReplacement>? ReturnOrReplacements { get; set; }

    //return display id
    public string GetDisplayId()
    {
        string result = $"{this.Id}";
        while (result.Length < 8)
        {
            result = "0" + result;
        }
        return result;
    }
}