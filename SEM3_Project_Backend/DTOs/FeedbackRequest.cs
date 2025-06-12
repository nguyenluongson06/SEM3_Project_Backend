namespace SEM3_Project_Backend.DTOs;

public class FeedbackRequest
{
    public int CustomerId { get; set; }
    public string ProductId { get; set; }
    public string Message { get; set; }
    public int Rating { get; set; }
}
