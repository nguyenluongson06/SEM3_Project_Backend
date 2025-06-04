namespace SEM3_Project_Backend.Model;

public enum PaymentType
{
    CreditCard,
    Cheque, 
    VPP
}

//temp class for payment info, subjected to change
public class Payment
{
    public int Id { get; set; }
    
    //1-1 to order
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    
    public PaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    
    //should this be kept encrypted in DB or not kept at all?
    public string? CardDetails { get; set; }
    public string? ChequeDetails { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
}