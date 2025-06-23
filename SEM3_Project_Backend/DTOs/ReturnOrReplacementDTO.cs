namespace SEM3_Project_Backend.DTOs;

public class ReturnOrReplacementDTO
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string ProductId { get; set; }
    public string RequestType { get; set; }
    public string ApprovalStatus { get; set; }
    public DateTime RequestDate { get; set; }
}