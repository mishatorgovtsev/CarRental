using CarRental.App.Data;
using CarRental.App.Models;
using CarRental.App.Services;
using System.Windows;

namespace CarRental.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // КОСТЫЛЬ: окна входа пока нет, поэтому заполняем БД тестовыми данными при запуске (если она пустая)
        try
        {
            using var db = new AppDbContext();
            SeedData.Fill(db);
            // ВРЕМЕННО для проверки этапа 3: бронь -> выдача -> возврат. Запустить ОДИН раз, потом удалить
            new AuthService(db).Login("manager1", "manager123");
            var client = db.Clients.First(c => c.LastName == "Петров");
            var car = db.Cars.First(c => c.PlateNumber == "А123ВС 33");
            var start = DateTime.Now.AddMinutes(5);
            var booking = new BookingService(db).CreateBooking(client.Id, car.Id, start, start.AddHours(24), null);
            var rental = new RentalService(db);
            var contract = rental.IssueCar(booking.Id, true, 48200, 80, null, null, "г. Владимир, ул. Мира, 1", PaymentMethod.Card);
            var calc = rental.ReturnCar(contract.Id, contract.IssuedAt.AddHours(25.5), 48540, 60, null, null, 500, "просрочка", 300, "мойка", PaymentMethod.Card);
            MessageBox.Show($"Аренда {calc.Rent}, итого {calc.Total}, доплата {calc.Surcharge}, залог {calc.DepositToRefund}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка работы с БД: " + ex.Message);
        }
        base.OnStartup(e);
    }
}