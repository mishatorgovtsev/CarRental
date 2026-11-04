namespace CarRental.App.Models;

public class Maintenance
{
    public int Id { get; set; }
    public int CarId { get; set; }
    public MaintenanceType Type { get; set; }
    public DateTime Date { get; set; }
    public DateTime? ValidUntil { get; set; }   // для страховки и техосмотра
    public decimal? Cost { get; set; }
    public string? Comment { get; set; }

    public Car? Car { get; set; }
}