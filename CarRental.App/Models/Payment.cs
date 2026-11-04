namespace CarRental.App.Models;

public class Payment
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int UserId { get; set; }             // сотрудник, принявший платёж
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentType Type { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Comment { get; set; }

    public RentalContract? Contract { get; set; }
    public User? User { get; set; }
}