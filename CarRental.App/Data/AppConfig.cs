using Microsoft.Extensions.Configuration;

namespace CarRental.App.Data;

// Читает appsettings.json из папки с программой
public static class AppConfig
{
    private static readonly IConfigurationRoot config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    public static string ConnectionString =>
        config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("В appsettings.json нет строки подключения Default");

    public static int MinRentalMinutes =>
        int.TryParse(config["MinRentalMinutes"], out var m) ? m : 60;
}