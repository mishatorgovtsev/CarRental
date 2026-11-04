namespace CarRental.App.Models;

public class Tariff
{
    public int Id { get; set; }
    public CarClass CarClass { get; set; }
    public string Name { get; set; } = "";
    public decimal PricePerHour { get; set; }
    public decimal Deposit { get; set; }
    public bool IsActive { get; set; } = true;
}