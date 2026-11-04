using CarRental.App.Data;
using CarRental.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.App.Services;

// Результат расчёта при возврате (его же показываем в блоке «Расчёт» до закрытия договора)
public class ReturnCalculation
{
    public int Minutes { get; set; }
    public decimal Rent { get; set; }            // аренда по фактическому времени (со скидкой)
    public decimal Penalties { get; set; }
    public decimal ExtraServices { get; set; }
    public decimal Total { get; set; }           // аренда + штрафы + допуслуги
    public decimal AlreadyPaid { get; set; }     // оплачено за аренду при выдаче
    public decimal Surcharge { get; set; }       // к доплате
    public decimal Overpayment { get; set; }     // переплата при досрочном возврате
    public decimal DepositToRefund { get; set; } // залог к возврату отдельным платежом
}

// Выдача и возврат автомобиля
public class RentalService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;
    private readonly CatalogService _catalog;

    public RentalService(AppDbContext db)
    {
        _db = db;
        _audit = new AuditService(db);
        _catalog = new CatalogService(db);
    }

    // Поиск броней со статусом «создана» по номеру брони или фамилии клиента (окно выдачи)
    public List<Booking> FindBookingsForIssue(string text)
    {
        text = (text ?? "").Trim();
        var query = _db.Bookings.Include(b => b.Client).Include(b => b.Car)
                       .Where(b => b.Status == BookingStatus.Created);
        if (int.TryParse(text, out int id))
            query = query.Where(b => b.Id == id);
        else if (text.Length > 0)
            query = query.Where(b => b.Client!.LastName.Contains(text));
        return query.OrderBy(b => b.StartAt).ToList();
    }

    // Открытые договоры (окно возврата)
    public List<RentalContract> GetOpenContracts()
        => _db.RentalContracts
              .Include(c => c.Booking).ThenInclude(b => b!.Client)
              .Include(c => c.Booking).ThenInclude(b => b!.Car)
              .Where(c => c.Status == ContractStatus.Open)
              .OrderBy(c => c.IssuedAt)
              .ToList();

    // Выдача: акт осмотра, договор, платежи (аренда и залог), статусы брони и автомобиля
    public RentalContract IssueCar(int bookingId, bool documentsChecked, int mileage, int fuelPercent,
                                   string? damages, string? photoPath, string office, PaymentMethod method)
    {
        var user = Session.CurrentUser;
        if (user == null || !Session.IsStaff)
            throw new BusinessException("Выдавать автомобили могут менеджер и администратор");
        if (!documentsChecked)
            throw new BusinessException("Подтвердите проверку документов клиента");
        office = (office ?? "").Trim();
        if (office.Length == 0)
            throw new BusinessException("Укажите пункт выдачи и возврата");
        if (fuelPercent < 0 || fuelPercent > 100)
            throw new BusinessException("Уровень топлива должен быть от 0 до 100");

        var booking = _db.Bookings.Include(b => b.Client).Include(b => b.Car)
                          .FirstOrDefault(b => b.Id == bookingId)
                      ?? throw new BusinessException("Бронь не найдена");
        if (booking.Status != BookingStatus.Created)
            throw new BusinessException("Выдать можно только бронь со статусом «создана»");

        var car = booking.Car!;
        var client = booking.Client!;
        if (client.LicenseExpiryDate.Date < booking.EndAt.Date)
            throw new BusinessException("Срок действия прав истекает раньше конца аренды");
        if (car.Status == CarStatus.Maintenance || car.Status == CarStatus.WrittenOff)
            throw new BusinessException("Автомобиль сейчас недоступен для выдачи");
        if (car.Status == CarStatus.Issued)
            throw new BusinessException("Автомобиль ещё не возвращён предыдущим клиентом");
        if (mileage < car.Mileage)
            throw new BusinessException("Пробег не может быть меньше текущего пробега автомобиля");

        var tariff = _catalog.GetActiveTariff(car.CarClass)
                     ?? throw new BusinessException("Для класса автомобиля нет действующего тарифа");

        using var tx = _db.Database.BeginTransaction();
        var now = DateTime.Now;

        var contract = new RentalContract
        {
            BookingId = booking.Id,
            ManagerId = user.Id,
            ContractNumber = $"ДА-{booking.Id:0000}",   // номер договора = номер брони (бронь одна на договор)
            Office = office,
            IssuedAt = now,
            Deposit = tariff.Deposit,
            Status = ContractStatus.Open
        };
        _db.RentalContracts.Add(contract);

        contract.Acts.Add(new InspectionAct
        {
            UserId = user.Id,
            Type = ActType.Issue,
            Mileage = mileage,
            FuelPercent = fuelPercent,
            Damages = damages,
            PhotoPath = photoPath,
            CreatedAt = now
        });

        if (booking.EstimatedCost > 0)
            contract.Payments.Add(new Payment
            {
                UserId = user.Id,
                Amount = booking.EstimatedCost,
                Method = method,
                Type = PaymentType.Rent,
                PaidAt = now,
                Comment = "Оплата аренды при выдаче"
            });
        if (tariff.Deposit > 0)
            contract.Payments.Add(new Payment
            {
                UserId = user.Id,
                Amount = tariff.Deposit,
                Method = method,
                Type = PaymentType.Deposit,
                PaidAt = now,
                Comment = "Залог"
            });

        booking.Status = BookingStatus.Issued;
        car.Status = CarStatus.Issued;
        car.Mileage = mileage;

        _db.SaveChanges();
        _audit.Log("Выдача автомобиля", $"Договор {contract.ContractNumber}, автомобиль {car.PlateNumber}", user.Id);
        tx.Commit();
        return contract;
    }

    // Расчёт при возврате (ничего не сохраняет): для блока «Расчёт» в окне возврата
    public ReturnCalculation CalculateReturn(int contractId, DateTime returnedAt, decimal penalties, decimal extraServices)
    {
        var contract = LoadContract(contractId);
        return Calc(contract, returnedAt, penalties, extraServices);
    }

    // Возврат: акт, итог, доплата, возврат залога, закрытие договора
    public ReturnCalculation ReturnCar(int contractId, DateTime returnedAt, int mileage, int fuelPercent,
                                       string? damages, string? photoPath,
                                       decimal penalties, string? penaltiesComment,
                                       decimal extraServices, string? extraServicesComment,
                                       PaymentMethod method)
    {
        var user = Session.CurrentUser;
        if (user == null || !Session.IsStaff)
            throw new BusinessException("Принимать автомобили могут менеджер и администратор");
        if (penalties < 0 || extraServices < 0)
            throw new BusinessException("Суммы штрафов и допуслуг не могут быть отрицательными");
        if (fuelPercent < 0 || fuelPercent > 100)
            throw new BusinessException("Уровень топлива должен быть от 0 до 100");

        var contract = LoadContract(contractId);
        if (contract.Status != ContractStatus.Open)
            throw new BusinessException("Договор уже закрыт");
        if (returnedAt < contract.IssuedAt)
            throw new BusinessException("Время возврата не может быть раньше времени выдачи");

        var car = contract.Booking!.Car!;
        if (mileage < car.Mileage)
            throw new BusinessException("Пробег при возврате не может быть меньше пробега при выдаче");

        using var tx = _db.Database.BeginTransaction();
        var now = DateTime.Now;
        var calc = Calc(contract, returnedAt, penalties, extraServices);

        contract.ActualReturnAt = returnedAt;
        contract.Penalties = penalties;
        contract.PenaltiesComment = penaltiesComment;
        contract.ExtraServices = extraServices;
        contract.ExtraServicesComment = extraServicesComment;
        contract.TotalCost = calc.Total;
        contract.Status = ContractStatus.Closed;

        _db.InspectionActs.Add(new InspectionAct
        {
            ContractId = contract.Id,
            UserId = user.Id,
            Type = ActType.Return,
            Mileage = mileage,
            FuelPercent = fuelPercent,
            Damages = damages,
            PhotoPath = photoPath,
            CreatedAt = now
        });

        if (calc.Surcharge > 0)
            _db.Payments.Add(new Payment
            {
                ContractId = contract.Id,
                UserId = user.Id,
                Amount = calc.Surcharge,
                Method = method,
                Type = PaymentType.Rent,
                PaidAt = now,
                Comment = "Доплата при возврате"
            });
        // Залог возвращается полностью отдельным платежом
        if (calc.DepositToRefund > 0)
            _db.Payments.Add(new Payment
            {
                ContractId = contract.Id,
                UserId = user.Id,
                Amount = calc.DepositToRefund,
                Method = method,
                Type = PaymentType.DepositRefund,
                PaidAt = now,
                Comment = "Возврат залога"
            });

        contract.Booking!.Status = BookingStatus.Completed;
        // КОСТЫЛЬ: после возврата ставим «свободен» (доступность считается по Booking)
        car.Status = CarStatus.Free;
        car.Mileage = mileage;

        _db.SaveChanges();
        _audit.Log("Возврат автомобиля", $"Договор {contract.ContractNumber}, итог {calc.Total} ₽", user.Id);
        tx.Commit();
        return calc;
    }

    private RentalContract LoadContract(int contractId)
        => _db.RentalContracts
              .Include(c => c.Booking).ThenInclude(b => b!.Car)
              .Include(c => c.Booking).ThenInclude(b => b!.Discount)
              .FirstOrDefault(c => c.Id == contractId)
           ?? throw new BusinessException("Договор не найден");

    private ReturnCalculation Calc(RentalContract contract, DateTime returnedAt, decimal penalties, decimal extra)
    {
        var booking = contract.Booking!;
        // Берём действующий тариф класса (цена за час в брони не хранится)
        var tariff = _catalog.GetActiveTariff(booking.Car!.CarClass)
                     ?? throw new BusinessException("Для класса автомобиля нет действующего тарифа");

        int min = AppConfig.MinRentalMinutes;
        decimal rent = PriceCalculator.CalculateRent(tariff.PricePerHour, contract.IssuedAt, returnedAt, booking.Discount, min);
        decimal paid = _db.Payments.Where(p => p.ContractId == contract.Id && p.Type == PaymentType.Rent)
                                   .Sum(p => p.Amount);
        decimal total = rent + penalties + extra;

        return new ReturnCalculation
        {
            Minutes = PriceCalculator.CountMinutes(contract.IssuedAt, returnedAt, min),
            Rent = rent,
            Penalties = penalties,
            ExtraServices = extra,
            Total = total,
            AlreadyPaid = paid,
            Surcharge = Math.Max(0, total - paid),
            // КОСТЫЛЬ: переплата при досрочном возврате только показывается, деньги не возвращаются
            Overpayment = Math.Max(0, paid - total),
            DepositToRefund = contract.Deposit
        };
    }
}