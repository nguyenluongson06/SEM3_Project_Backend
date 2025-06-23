namespace SEM3_Project_Backend.DTOs;

public class OrderRequest
{
    public int CustomerId { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
}
