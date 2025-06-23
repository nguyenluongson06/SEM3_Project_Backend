namespace SEM3_Project_Backend.DTOs;

public class FeedbackRequest
{
    public string? ProductId { get; set; }
    public string Message { get; set; } = string.Empty;
}
