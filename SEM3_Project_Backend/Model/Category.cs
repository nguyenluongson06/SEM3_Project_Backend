namespace SEM3_Project_Backend.Model;

public class Category
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public ICollection<Product>? Products { get; set; }
}