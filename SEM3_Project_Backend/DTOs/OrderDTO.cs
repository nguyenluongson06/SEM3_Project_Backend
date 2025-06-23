namespace SEM3_Project_Backend.DTOs;
public class OrderDTO
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public float TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string DispatchStatus { get; set; } = "Pending";
    public DateTime DeliveryDate { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? DeliveryType { get; set; }
    public List<OrderItemDTO> Items { get; set; } = new();
}