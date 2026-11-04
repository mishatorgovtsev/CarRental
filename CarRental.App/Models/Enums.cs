namespace CarRental.App.Models;

public enum UserRole { Admin = 0, Manager = 1, Client = 2 }

public enum CarClass { Economy = 0, Comfort = 1, Business = 2 }

public enum CarStatus { WrittenOff = 0, Free = 1, Booked = 2, Issued = 3, Maintenance = 4 }

public enum BookingStatus { Created = 0, Issued = 1, Completed = 2, Cancelled = 3 }

public enum ContractStatus { Closed = 0, Open = 1 }

public enum PaymentMethod { Cash = 0, Card = 1, Transfer = 2 }

// Эти три enum хранятся в БД строкой (Nvarchar(20)), см. AppDbContext
public enum PaymentType { Rent, Deposit, DepositRefund, Penalty }

public enum MaintenanceType { Service, Repair, Insurance, Inspection }

public enum ActType { Issue, Return }