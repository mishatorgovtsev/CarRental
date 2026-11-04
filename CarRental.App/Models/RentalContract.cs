namespace CarRental.App.Models;

public class RentalContract
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ManagerId { get; set; }          // ссылка на User
    public string ContractNumber { get; set; } = "";
    public string Office { get; set; } = "";    // пункт выдачи и возврата
    public DateTime IssuedAt { get; set; }
    public DateTime? ActualReturnAt { get; set; }
    public decimal Deposit { get; set; }
    public decimal Penalties { get; set; }
    public string? PenaltiesComment { get; set; }
    public decimal ExtraServices { get; set; }
    public string? ExtraServicesComment { get; set; }
    public decimal? TotalCost { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Open;
    public string? PdfPath { get; set; }       

    public Booking? Booking { get; set; }
    public User? Manager { get; set; }
    public List<Payment> Payments { get; set; } = new();
    public List<InspectionAct> Acts { get; set; } = new();
}