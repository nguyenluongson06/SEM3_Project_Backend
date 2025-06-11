namespace SEM3_Project_Backend.Model;

public class Product
{
    //format: 2 digit category + 5 digit product id
    //e.g.: "AB12345"
    public required string Id { get; set; }
    
    public string? Name { get; set; }
    public string? Description { get; set; }
    public float Price { get; set; }
    
    //should be removed, use Inventory instead
    public int StockQuantity { get; set; }
    
    //many-1 category
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    
    //should be month(s)
    public int WarrantyPeriod { get; set; }
    
    //are these needed?
    public ICollection<OrderItem>? OrderItems { get; set; }
    public ICollection<ReturnOrReplacement>? ReturnOrReplacements { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    //inventory item for inventory management
    public InventoryItem? InventoryItem { get; set; }
}