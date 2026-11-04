namespace CarRental.App.Models;

public class Booking
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int CarId { get; set; }
    public int? DiscountId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Created;
    public decimal EstimatedCost { get; set; }
    public DateTime CreatedAt { get; set; }

    public Client? Client { get; set; }
    public Car? Car { get; set; }
    public Discount? Discount { get; set; }
    public RentalContract? Contract { get; set; }
}