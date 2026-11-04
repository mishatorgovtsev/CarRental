namespace CarRental.App.Models;

public class Discount
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int? Percent { get; set; }            // целые проценты 0..100
    public decimal? FixedAmount { get; set; }    // заполнено одно из двух полей
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public bool IsActive { get; set; } = true;
}