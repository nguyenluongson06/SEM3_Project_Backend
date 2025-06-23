namespace SEM3_Project_Backend.DTOs;

public class OrderItemDTO
{
    public string ProductId { get; set; }
    public string? ProductName { get; set; }
    public int Quantity { get; set; }
    public float Price { get; set; }
}