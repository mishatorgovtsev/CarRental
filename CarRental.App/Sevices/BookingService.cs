using CarRental.App.Data;
using CarRental.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.App.Services;

// Бронирование: создание, отмена, список броней клиента
public class BookingService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly CatalogService _catalog;

    public BookingService(AppDbContext db)
    {
        _db = db;
        _audit = new AuditService(db);
        _catalog = new CatalogService(db);
    }

    // Предварительная стоимость для показа перед подтверждением (ничего не сохраняет)
    public decimal EstimateCost(int carId, DateTime start, DateTime end, int? discountId)
    {
        if (end <= start) throw new BusinessException("Конец аренды должен быть позже начала");
        var car = GetCar(carId);
        var tariff = GetTariff(car);
        var discount = GetDiscount(discountId, start);
        return PriceCalculator.CalculateRent(tariff.PricePerHour, start, end, discount, AppConfig.MinRentalMinutes);
    }

    // Создание брони. Порядок проверок как на диаграмме деятельности: права, потом занятость автомобиля
    public Booking CreateBooking(int clientId, int carId, DateTime start, DateTime end, int? discountId)
    {
        var user = Session.CurrentUser ?? throw new BusinessException("Войдите в систему");
        var client = _db.Clients.Find(clientId) ?? throw new BusinessException("Клиент не найден");
        if (user.Role == UserRole.Client && client.UserId != user.Id)
            throw new BusinessException("Клиент может бронировать только на себя");

        if (end <= start)
            throw new BusinessException("Конец аренды должен быть позже начала");
        if (start < DateTime.Now.AddMinutes(-5))
            throw new BusinessException("Начало аренды не может быть в прошлом");
        if (client.LicenseExpiryDate.Date < end.Date)
            throw new BusinessException("Срок действия прав истекает");

        var car = GetCar(carId);
        if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.WrittenOff)
            throw new BusinessException("Автомобиль недоступен для бронирования");
        var tariff = GetTariff(car);
        var discount = GetDiscount(discountId, start);
        decimal cost = PriceCalculator.CalculateRent(tariff.PricePerHour, start, end, discount, AppConfig.MinRentalMinutes);

        // Проверка пересечения и создание брони в одной транзакции.
        // Serializable, чтобы две брони одновременно не прошли проверку
        using var tx = _db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);

        if (!_catalog.IsCarAvailable(carId, start, end))
            throw new BusinessException("Автомобиль занят на выбранный период");

        var booking = new Booking
        {
            ClientId = clientId,
            CarId = carId,
            DiscountId = discountId,
            StartAt = start,
            EndAt = end,
            Status = BookingStatus.Created,
            EstimatedCost = cost,
            CreatedAt = DateTime.Now
        };
        _db.Bookings.Add(booking);

        // КОСТЫЛЬ: статус автомобиля только информационный; не понижаем «выдан» до «забронирован»
        if (car.Status == CarStatus.Free)
            car.Status = CarStatus.Booked;

        _db.SaveChanges();
        _audit.Log("Создана бронь", $"Бронь №{booking.Id}, автомобиль {car.PlateNumber}", user.Id);
        tx.Commit();
        return booking;
    }

    // Отмена брони: клиент (свою) или сотрудник, пока статус «создана»
    public void CancelBooking(int bookingId)
    {
        var user = Session.CurrentUser ?? throw new BusinessException("Войдите в систему");
        var booking = _db.Bookings.Include(b => b.Client).Include(b => b.Car)
                          .FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new BusinessException("Бронь не найдена");

        if (user.Role == UserRole.Client && booking.Client!.UserId != user.Id)
            throw new BusinessException("Можно отменять только свои брони");
        if (booking.Status != BookingStatus.Created)
            throw new BusinessException("Отменить можно только бронь со статусом «создана»");

        booking.Status = BookingStatus.Cancelled;

        // КОСТЫЛЬ: освобождаем статус автомобиля, если других неоформленных броней нет
        var car = booking.Car!;
        bool otherCreated = _db.Bookings.Any(b => b.CarId == car.Id && b.Id != booking.Id
                                                  && b.Status == BookingStatus.Created);
        if (car.Status == CarStatus.Booked && !otherCreated)
            car.Status = CarStatus.Free;

        _db.SaveChanges();
        _audit.Log("Отмена брони", $"Бронь №{booking.Id}, автомобиль {car.PlateNumber}");
    }

    // Брони клиента вместе с автомобилем и договором (для «Мои аренды»)
    public List<Booking> GetClientBookings(int clientId)
        => _db.Bookings.Include(b => b.Car).Include(b => b.Contract)
              .Where(b => b.ClientId == clientId)
              .OrderByDescending(b => b.StartAt)
              .ToList();

    private Car GetCar(int id)
        => _db.Cars.Find(id) ?? throw new BusinessException("Автомобиль не найден");

    private Tariff GetTariff(Car car)
        => _catalog.GetActiveTariff(car.CarClass)
           ?? throw new BusinessException("Для класса автомобиля нет действующего тарифа");

    private Discount? GetDiscount(int? id, DateTime onDate)
    {
        if (id == null) return null;
        var d = _db.Discounts.Find(id.Value) ?? throw new BusinessException("Скидка не найдена");
        if (!d.IsActive || d.DateFrom > onDate.Date || d.DateTo < onDate.Date)
            throw new BusinessException("Скидка не действует на выбранную дату");
        return d;
    }
}