namespace CarRental.App.Models;

public class InspectionAct
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public int UserId { get; set; }
    public ActType Type { get; set; }
    public int Mileage { get; set; }
    public int FuelPercent { get; set; }
    public string? Damages { get; set; }
    // ЗАГЛУШКА: фотофиксации нет, хранится только путь к файлу
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; }

    public RentalContract? Contract { get; set; }
    public User? User { get; set; }
}