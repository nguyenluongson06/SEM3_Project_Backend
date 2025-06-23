namespace SEM3_Project_Backend.DTOs;
public class OrderDTO
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public float TotalAmount { get; set; }
    public string PaymentStatus { get; set; }
    public string DispatchStatus { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryType { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
}