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
       
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка работы с БД: " + ex.Message);
        }
        base.OnStartup(e);
    }
}