using CarRental.App.Models;

namespace CarRental.App.Services;

// Расчёт стоимости. Ничего не знает о БД
public static class PriceCalculator
{
    // Минуты аренды: округление вверх до целой минуты, но не меньше minMinutes
    public static int CountMinutes(DateTime start, DateTime end, int minMinutes)
    {
        long ticks = (end - start).Ticks;
        if (ticks < 0) ticks = 0;
        int minutes = (int)((ticks + TimeSpan.TicksPerMinute - 1) / TimeSpan.TicksPerMinute);
        return Math.Max(minutes, minMinutes);
    }

    // Стоимость аренды: цена за час / 60 * минуты, затем скидка, итог не меньше 0, округление до копеек
    public static decimal CalculateRent(decimal pricePerHour, DateTime start, DateTime end,
                                        Discount? discount, int minMinutes)
    {
        int minutes = CountMinutes(start, end, minMinutes);
        decimal cost = pricePerHour * minutes / 60m;   // сначала умножаем, потом делим, чтобы не терять точность

        if (discount != null)
        {
            if (discount.Percent != null)
                cost -= cost * discount.Percent.Value / 100m;
            else if (discount.FixedAmount != null)
                cost -= discount.FixedAmount.Value;
        }

        if (cost < 0) cost = 0;
        return Math.Round(cost, 2, MidpointRounding.AwayFromZero);
    }
}