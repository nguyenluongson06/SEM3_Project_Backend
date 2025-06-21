namespace SEM3_Project_Backend.Model;

public enum UserRequestType
{
    Return, 
    Replacement
}

public enum UserRequestApprovalStatus
{
    Pending, 
    Approved,
    Rejected
}

public class ReturnOrReplacement
{
    //TODO: should use DTO instead of model directly
    public int Id { get; set; }
    
    //1-1 order
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    //1-1 product
    public required string ProductId { get; set; }
    public Product? Product { get; set; }
    
    public UserRequestType RequestType { get; set; }
    
    public DateTime RequestDate { get; set; }
    public UserRequestApprovalStatus ApprovalStatus { get; set; }
}