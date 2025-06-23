namespace SEM3_Project_Backend.DTOs;

public class FeedbackDTO
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public required string ProductId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}