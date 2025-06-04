namespace SEM3_Project_Backend.Model;

public class Product
{
    //format: 2 digit category + 5 digit product id
    public required string Id { get; set; }
    
    public string? Name { get; set; }
    public string? Description { get; set; }
    public float Price { get; set; }
    public int StockQuantity { get; set; }
    
    //many-1 category
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    
    //should be month(s)
    public int WarrantyPeriod { get; set; }
}