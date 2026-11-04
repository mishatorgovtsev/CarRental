using CarRental.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.App.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<RentalContract> RentalContracts => Set<RentalContract>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();
    public DbSet<InspectionAct> InspectionActs => Set<InspectionAct>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
            options.UseSqlServer(AppConfig.ConnectionString);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.Property(x => x.Login).HasMaxLength(50);
            e.Property(x => x.PasswordHash).HasMaxLength(100);
            e.Property(x => x.FullName).HasMaxLength(150);
            // HasSentinel(true) нужен, чтобы EF не заменял false на значение по умолчанию
            e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasIndex(x => x.Login).IsUnique();
        });

        b.Entity<Client>(e =>
        {
            e.Property(x => x.LastName).HasMaxLength(50);
            e.Property(x => x.FirstName).HasMaxLength(50);
            e.Property(x => x.MiddleName).HasMaxLength(50);
            e.Property(x => x.BirthDate).HasColumnType("date");
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(100);
            e.Property(x => x.PassportSeriesNumber).HasMaxLength(20);
            e.Property(x => x.PassportIssuedBy).HasMaxLength(200);
            e.Property(x => x.PassportIssueDate).HasColumnType("date");
            e.Property(x => x.LicenseNumber).HasMaxLength(20);
            e.Property(x => x.LicenseCategory).HasMaxLength(10);
            e.Property(x => x.LicenseIssueDate).HasColumnType("date");
            e.Property(x => x.LicenseExpiryDate).HasColumnType("date");
            // один к одному: уникальность Client.UserId создаётся автоматически
            e.HasOne(x => x.User).WithOne(u => u.Client).HasForeignKey<Client>(x => x.UserId);
        });

        b.Entity<Car>(e =>
        {
            e.Property(x => x.Brand).HasMaxLength(50);
            e.Property(x => x.Model).HasMaxLength(50);
            e.Property(x => x.Generation).HasMaxLength(50);
            e.Property(x => x.PlateNumber).HasMaxLength(15);
            e.Property(x => x.Vin).HasMaxLength(17);
            e.Property(x => x.PhotoPath).HasMaxLength(260);
            e.HasIndex(x => x.PlateNumber).IsUnique();
            e.HasIndex(x => x.Vin).IsUnique();
        });

        b.Entity<Tariff>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.PricePerHour).HasPrecision(10, 2);
            e.Property(x => x.Deposit).HasPrecision(10, 2);
            e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            // один активный тариф на класс автомобиля
            e.HasIndex(x => x.CarClass).IsUnique().HasFilter("[IsActive] = 1");
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Tariff_PricePerHour", "[PricePerHour] >= 0");
                t.HasCheckConstraint("CK_Tariff_Deposit", "[Deposit] >= 0");
            });
        });

        b.Entity<Discount>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.FixedAmount).HasPrecision(10, 2);
            e.Property(x => x.DateFrom).HasColumnType("date");
            e.Property(x => x.DateTo).HasColumnType("date");
            e.Property(x => x.IsActive).HasDefaultValue(true).HasSentinel(true);
            e.ToTable(t =>
            {
                // [Percent] в скобках, потому что PERCENT зарезервировано в T-SQL
                t.HasCheckConstraint("CK_Discount_Percent", "[Percent] BETWEEN 0 AND 100");
                t.HasCheckConstraint("CK_Discount_FixedAmount", "[FixedAmount] >= 0");
                t.HasCheckConstraint("CK_Discount_OneOf",
                    "([Percent] IS NOT NULL AND [FixedAmount] IS NULL) OR ([Percent] IS NULL AND [FixedAmount] IS NOT NULL)");
                t.HasCheckConstraint("CK_Discount_Dates", "[DateFrom] <= [DateTo]");
            });
        });

        b.Entity<Booking>(e =>
        {
            e.Property(x => x.EstimatedCost).HasPrecision(10, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Client).WithMany(c => c.Bookings).HasForeignKey(x => x.ClientId);
            e.HasOne(x => x.Car).WithMany(c => c.Bookings).HasForeignKey(x => x.CarId);
            e.HasOne(x => x.Discount).WithMany().HasForeignKey(x => x.DiscountId);
            e.ToTable(t => t.HasCheckConstraint("CK_Booking_Dates", "[EndAt] > [StartAt]"));
        });

        b.Entity<RentalContract>(e =>
        {
            e.Property(x => x.ContractNumber).HasMaxLength(20);
            e.Property(x => x.Office).HasMaxLength(200);
            e.Property(x => x.Deposit).HasPrecision(10, 2);
            e.Property(x => x.Penalties).HasPrecision(10, 2).HasDefaultValue(0m);
            e.Property(x => x.PenaltiesComment).HasMaxLength(200);
            e.Property(x => x.ExtraServices).HasPrecision(10, 2).HasDefaultValue(0m);
            e.Property(x => x.ExtraServicesComment).HasMaxLength(200);
            e.Property(x => x.TotalCost).HasPrecision(10, 2);
            e.Property(x => x.PdfPath).HasMaxLength(260);
            e.HasIndex(x => x.ContractNumber).IsUnique();
            // один договор на одну бронь: уникальность BookingId создаётся автоматически
            e.HasOne(x => x.Booking).WithOne(bk => bk.Contract).HasForeignKey<RentalContract>(x => x.BookingId);
            e.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Comment).HasMaxLength(200);
            e.HasOne(x => x.Contract).WithMany(c => c.Payments).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.ToTable(t => t.HasCheckConstraint("CK_Payment_Amount", "[Amount] > 0"));
        });

        b.Entity<InspectionAct>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Damages).HasMaxLength(500);
            e.Property(x => x.PhotoPath).HasMaxLength(260);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Contract).WithMany(c => c.Acts).HasForeignKey(x => x.ContractId);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.ToTable(t => t.HasCheckConstraint("CK_InspectionAct_Fuel", "[FuelPercent] BETWEEN 0 AND 100"));
        });

        b.Entity<Maintenance>(e =>
        {
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Date).HasColumnType("date");
            e.Property(x => x.ValidUntil).HasColumnType("date");
            e.Property(x => x.Cost).HasPrecision(10, 2);
            e.Property(x => x.Comment).HasMaxLength(200);
            e.HasOne(x => x.Car).WithMany(c => c.Maintenances).HasForeignKey(x => x.CarId);
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.Details).HasMaxLength(500);
            e.Property(x => x.At).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        // Без каскадного удаления: везде запрещаем удалять запись, на которую есть ссылки
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}