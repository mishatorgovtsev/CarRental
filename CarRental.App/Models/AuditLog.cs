namespace CarRental.App.Models;

public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }            // пусто для системных действий
    public DateTime At { get; set; }
    public string Action { get; set; } = "";    
    public string? Details { get; set; }

    public User? User { get; set; }
}