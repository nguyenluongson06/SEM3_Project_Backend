namespace SEM3_Project_Backend.Model;

//should only be created directly in db
public class Admin
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? Name { get; set; }
}