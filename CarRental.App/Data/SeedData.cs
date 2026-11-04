using CarRental.App.Models;

namespace CarRental.App.Data;

public static class SeedData
{
    private static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public static void Fill(AppDbContext db)
    {
        if (db.Users.Any()) return; // уже заполнено

        // Пользователи (пароли для проверки: admin123, manager123, client123)
        var admin = new User { Login = "admin", PasswordHash = Hash("admin123"), FullName = "Администратор Системы", Role = UserRole.Admin };
        var manager = new User { Login = "manager1", PasswordHash = Hash("manager123"), FullName = "Иванов Иван Иванович", Role = UserRole.Manager };
        var clientUser1 = new User { Login = "petrov", PasswordHash = Hash("client123"), FullName = "Петров Пётр Петрович", Role = UserRole.Client };
        var clientUser2 = new User { Login = "sidorova", PasswordHash = Hash("client123"), FullName = "Сидорова Анна Сергеевна", Role = UserRole.Client };
        db.Users.AddRange(admin, manager, clientUser1, clientUser2);

        db.Clients.AddRange(
            new Client
            {
                User = clientUser1,
                LastName = "Петров",
                FirstName = "Пётр",
                MiddleName = "Петрович",
                BirthDate = new DateTime(1990, 5, 12),
                Phone = "+7 900 111-11-11",
                Email = "petrov@mail.ru",
                PassportSeriesNumber = "1234 567890",
                PassportIssuedBy = "ОВД г. Владимира",
                PassportIssueDate = new DateTime(2010, 6, 1),
                LicenseNumber = "99 11 123456",
                LicenseCategory = "B",
                LicenseIssueDate = new DateTime(2015, 3, 15),
                LicenseExpiryDate = new DateTime(2030, 5, 12)
            },
            new Client
            {
                User = clientUser2,
                LastName = "Сидорова",
                FirstName = "Анна",
                MiddleName = "Сергеевна",
                BirthDate = new DateTime(1995, 9, 3),
                Phone = "+7 900 222-22-22",
                Email = "sidorova@mail.ru",
                PassportSeriesNumber = "4321 098765",
                PassportIssuedBy = "УМВД по Владимирской области",
                PassportIssueDate = new DateTime(2015, 9, 10),
                LicenseNumber = "99 22 654321",
                LicenseCategory = "B",
                LicenseIssueDate = new DateTime(2018, 4, 20),
                LicenseExpiryDate = new DateTime(2035, 3, 15)
            });

        // Автомобили (данные из макетов)
        var cars = new List<Car>
        {
            new() { Brand = "Kia", Model = "Rio", Generation = "IV (FB)", Year = 2021, CarClass = CarClass.Economy, PlateNumber = "А123ВС 33", Vin = "XWEFH41ABM0000001", Mileage = 48200 },
            new() { Brand = "Hyundai", Model = "Solaris", Year = 2020, CarClass = CarClass.Economy, PlateNumber = "В456КМ 33", Vin = "XWEFH41ABM0000002", Mileage = 61900 },
            new() { Brand = "Skoda", Model = "Octavia", Year = 2022, CarClass = CarClass.Comfort, PlateNumber = "Е789ТО 33", Vin = "XWEFH41ABM0000003", Mileage = 27400 },
            new() { Brand = "Toyota", Model = "Camry", Year = 2022, CarClass = CarClass.Business, PlateNumber = "К321РХ 33", Vin = "XWEFH41ABM0000004", Mileage = 19800 },
            new() { Brand = "Mazda", Model = "6", Year = 2021, CarClass = CarClass.Comfort, PlateNumber = "О987СТ 33", Vin = "XWEFH41ABM0000005", Mileage = 40600 },
            new() { Brand = "BMW", Model = "5", Year = 2023, CarClass = CarClass.Business, PlateNumber = "Р246УК 33", Vin = "XWEFH41ABM0000006", Mileage = 9300 },
            new() { Brand = "Volkswagen", Model = "Polo", Year = 2019, CarClass = CarClass.Economy, PlateNumber = "М654НА 33", Vin = "XWEFH41ABM0000007", Mileage = 83100, Status = CarStatus.Maintenance },
            new() { Brand = "Lada", Model = "Vesta", Year = 2018, CarClass = CarClass.Economy, PlateNumber = "С135ЕН 33", Vin = "XWEFH41ABM0000008", Mileage = 112000, Status = CarStatus.WrittenOff },
        };
        db.Cars.AddRange(cars);

        db.Tariffs.AddRange(
            new Tariff { CarClass = CarClass.Economy, Name = "Эконом — базовый", PricePerHour = 180, Deposit = 5000 },
            new Tariff { CarClass = CarClass.Comfort, Name = "Комфорт — базовый", PricePerHour = 300, Deposit = 8000 },
            new Tariff { CarClass = CarClass.Business, Name = "Бизнес — базовый", PricePerHour = 550, Deposit = 15000 });

        db.Discounts.AddRange(
            new Discount { Name = "Постоянный клиент", Percent = 10, DateFrom = new DateTime(2026, 1, 1), DateTo = new DateTime(2026, 12, 31) },
            new Discount { Name = "Выходные", Percent = 5, DateFrom = new DateTime(2026, 10, 1), DateTo = new DateTime(2026, 12, 31) },
            new Discount { Name = "Новый клиент", FixedAmount = 300, DateFrom = new DateTime(2026, 1, 1), DateTo = new DateTime(2026, 12, 31) },
            new Discount { Name = "Летняя акция", Percent = 15, DateFrom = new DateTime(2026, 6, 1), DateTo = new DateTime(2026, 8, 31), IsActive = false });

        db.Maintenances.AddRange(
            new Maintenance { Car = cars[6], Type = MaintenanceType.Repair, Date = new DateTime(2026, 10, 28), Cost = 12500, Comment = "Замена тормозных колодок" },
            new Maintenance { Car = cars[0], Type = MaintenanceType.Insurance, Date = new DateTime(2026, 1, 15), ValidUntil = new DateTime(2027, 1, 14), Cost = 18000, Comment = "ОСАГО" });

        db.SaveChanges();
    }
}