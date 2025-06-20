using System.Text;

namespace SEM3_Project_Backend.Model;

public enum PaymentStatus
{
    Pending, Cleared, Rejected
}

public enum DispatchStatus
{
    Pending, Dispatched, Delivered, Cancelled
}

public enum DeliveryType
{
    Standard, Express, SameDay
}

public class Order
{
    //use simple numeric id, 8-digit order number is only used for frontend display
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }

    //use enum above
    public DeliveryType DeliveryType { get; set; }
    
    //address should not be null, but can be empty
    //if empty, it means customer has not set delivery address yet
    public string DeliveryAddress { get; set; } = string.Empty;
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