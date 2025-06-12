namespace SEM3_Project_Backend.DTOs;

public class OrderRequest
{
    public int CustomerId { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
}

public class OrderItemDTO
{
    public string ProductId { get; set; }
    public int Quantity { get; set; }
}
