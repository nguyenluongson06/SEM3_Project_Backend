namespace SEM3_Project_Backend.Model;

public class OrderItem
{
    public int Id { get; set; }
    
    //internally: linked to order with id for simplicity
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    //1-1 to product
    public required string ProductId { get; set; }
    public Product? Product { get; set; }
    
    public int Quantity { get; set; }
    public float Price { get; set; }
    
    //format: 1 digit delivery type + 7 digit product id + 8 digit padded order id
    public string? DisplayOrderId { get; set; }
}