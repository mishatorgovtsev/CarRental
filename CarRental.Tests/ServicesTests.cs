using CarRental.App.Models;
using CarRental.App.Services;
using Xunit;

namespace CarRental.Tests;

public class ServicesTests
{
    private static readonly DateTime T10 = new(2026, 11, 10, 10, 0, 0);

    // --- Расчёт стоимости ---

    [Fact]
    public void Rent_24Hours_At180PerHour_Is4320()
    {
        var cost = PriceCalculator.CalculateRent(180m, T10, T10.AddHours(24), null, 60);
        Assert.Equal(4320m, cost);
    }

    [Fact]
    public void Rent_LessThanMinimum_IsChargedForMinimum()
    {
        var cost = PriceCalculator.CalculateRent(180m, T10, T10.AddMinutes(10), null, 60);
        Assert.Equal(180m, cost);   // 10 минут оплачиваются как 60
    }

    [Fact]
    public void Rent_PartialMinute_IsRoundedUp()
    {
        // 90 минут и 1 секунда -> 91 минута -> 180 * 91 / 60 = 273
        var cost = PriceCalculator.CalculateRent(180m, T10, T10.AddMinutes(90).AddSeconds(1), null, 60);
        Assert.Equal(273m, cost);
    }

    [Fact]
    public void Rent_PercentDiscount_10Percent()
    {
        var discount = new Discount { Name = "Тест", Percent = 10 };
        var cost = PriceCalculator.CalculateRent(180m, T10, T10.AddHours(24), discount, 60);
        Assert.Equal(3888m, cost);
    }

    [Fact]
    public void Rent_FixedDiscountBiggerThanCost_IsZero()
    {
        var discount = new Discount { Name = "Тест", FixedAmount = 5000m };
        var cost = PriceCalculator.CalculateRent(180m, T10, T10.AddHours(1), discount, 60);
        Assert.Equal(0m, cost);
    }

    [Fact]
    public void Rent_IsRoundedToKopecks()
    {
        // 550 * 61 / 60 = 559,1666... -> 559,17
        var cost = PriceCalculator.CalculateRent(550m, T10, T10.AddMinutes(61), null, 60);
        Assert.Equal(559.17m, cost);
    }

    // --- Пересечение броней ---

    [Fact]
    public void Periods_TouchingEnds_DoNotOverlap()
    {
        // [10:00, 12:00) и [12:00, 14:00)
        Assert.False(CatalogService.PeriodsOverlap(T10, T10.AddHours(2), T10.AddHours(2), T10.AddHours(4)));
    }

    [Fact]
    public void Periods_Intersecting_Overlap()
    {
        // [10:00, 12:00) и [11:00, 13:00)
        Assert.True(CatalogService.PeriodsOverlap(T10, T10.AddHours(2), T10.AddHours(1), T10.AddHours(3)));
    }
}   