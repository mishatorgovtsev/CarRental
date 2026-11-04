namespace CarRental.App.Models;

public class Client
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string LastName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string? MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string PassportSeriesNumber { get; set; } = "";
    public string PassportIssuedBy { get; set; } = "";
    public DateTime PassportIssueDate { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string LicenseCategory { get; set; } = "";
    public DateTime LicenseIssueDate { get; set; }
    public DateTime LicenseExpiryDate { get; set; }

    public User? User { get; set; }
    public List<Booking> Bookings { get; set; } = new();
}