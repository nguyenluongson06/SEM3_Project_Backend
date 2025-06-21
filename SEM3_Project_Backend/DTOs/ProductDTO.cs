namespace SEM3_Project_Backend.DTOs;

public class ProductDTO
{
    //TODO: remove in product creation? could be empty when creating a new product
    //format: 2 digit category + 5 digit product id
    public string? Id { get; set; } 
    public string Name { get; set; }
    public string? Description { get; set; }
    public float Price { get; set; }
    public string? ImageUrl { get; set; }
    public int WarrantyPeriod { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; } 
    public int InventoryQuantity { get; set; } 
}