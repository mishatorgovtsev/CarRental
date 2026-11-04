using CarRental.App.Data;
using CarRental.App.Models;

namespace CarRental.App.Services;

// Каталог: доступность автомобилей, тарифы, действующие скидки
public class CatalogService
{
    private readonly AppDbContext _db;

    public CatalogService(AppDbContext db) => _db = db;

    // Периоды [s1, e1) и [s2, e2) пересекаются, если каждый начинается раньше конца другого.
    // Совпадение концов пересечением не считается.
    // Тот же критерий записан в запросах ниже: в LINQ вызвать этот метод нельзя, поэтому он нужен для тестов
    public static bool PeriodsOverlap(DateTime s1, DateTime e1, DateTime s2, DateTime e2)
        => s1 < e2 && e1 > s2;

    // Свободен ли автомобиль на период. ignoreBookingId нужен, чтобы бронь не конфликтовала сама с собой
    public bool IsCarAvailable(int carId, DateTime start, DateTime end, int? ignoreBookingId = null)
    {
        var car = _db.Cars.Find(carId);
        if (car == null || car.Status == CarStatus.Maintenance || car.Status == CarStatus.WrittenOff)
            return false;

        bool busy = _db.Bookings.Any(b => b.CarId == carId
            && (b.Status == BookingStatus.Created || b.Status == BookingStatus.Issued)
            && b.StartAt < end && b.EndAt > start
            && (ignoreBookingId == null || b.Id != ignoreBookingId));
        return !busy;
    }

    // Каталог свободных автомобилей. Класс и период необязательны.
    // КОСТЫЛЬ: доступность считаем по таблице Booking, а не по Car.Status (статус только информационный)
    public List<Car> GetAvailableCars(CarClass? carClass, DateTime? start, DateTime? end)
    {
        var query = _db.Cars.Where(c => c.Status != CarStatus.Maintenance && c.Status != CarStatus.WrittenOff);

        if (carClass != null)
        {
            var cls = carClass.Value;
            query = query.Where(c => c.CarClass == cls);
        }

        if (start != null && end != null)
        {
            var s = start.Value;
            var e = end.Value;
            query = query.Where(c => !_db.Bookings.Any(b => b.CarId == c.Id
                && (b.Status == BookingStatus.Created || b.Status == BookingStatus.Issued)
                && b.StartAt < e && b.EndAt > s));
        }

        return query.OrderBy(c => c.Brand).ThenBy(c => c.Model).ToList();
    }

    // Действующий тариф класса автомобиля (связь Car - Tariff логическая, по CarClass)
    public Tariff? GetActiveTariff(CarClass carClass)
        => _db.Tariffs.FirstOrDefault(t => t.CarClass == carClass && t.IsActive);

    // Скидки, которые действуют на указанную дату
    public List<Discount> GetActiveDiscounts(DateTime onDate)
    {
        var date = onDate.Date;
        return _db.Discounts
            .Where(d => d.IsActive && d.DateFrom <= date && d.DateTo >= date)
            .OrderBy(d => d.Name)
            .ToList();
    }
}