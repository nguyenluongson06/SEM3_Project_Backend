namespace SEM3_Project_Backend.Model;

public class InventoryItem
{
    public int Id { get; set; }
    
    //1-1 to product
    public required string ProductId { get; set; }
    public Product? Product { get; set; }
    
    public int Quantity { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}