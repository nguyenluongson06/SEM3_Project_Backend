namespace SEM3_Project_Backend.Models;

public class Product
{
    public int Id { get; set; }
    public string? Name { get; set; }
    
    public string? ProductId { get; set; } //7 digit: 2 digit product code (should be category id, ex. 01) + 5 digit product number (ex. 00001)
    public string? Description { get; set; }
    public float Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; } //1-many to Category
    
    public Category? Category { get; set; }
}