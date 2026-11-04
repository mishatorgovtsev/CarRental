namespace CarRental.App.Models;

public class Car
{
    public int Id { get; set; }
    public string Brand { get; set; } = "";
    public string Model { get; set; } = "";
    public string? Generation { get; set; }
    public int Year { get; set; }
    public CarClass CarClass { get; set; }
    public string PlateNumber { get; set; } = "";
    public string Vin { get; set; } = "";
    public int Mileage { get; set; }
    public CarStatus Status { get; set; } = CarStatus.Free;
    // ЗАГЛУШКА: загрузка фото не реализована, хранится только путь к файлу
    public string? PhotoPath { get; set; }

    // Связи с Tariff нет: тариф подбирается по CarClass (логическая связь, без FK)
    public List<Booking> Bookings { get; set; } = new();
    public List<Maintenance> Maintenances { get; set; } = new();
}