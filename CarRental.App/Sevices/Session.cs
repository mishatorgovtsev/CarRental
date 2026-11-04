using CarRental.App.Models;

namespace CarRental.App.Services;

// Текущий вошедший пользователь
public static class Session
{
    public static User? CurrentUser { get; set; }

    public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;

    // Администратор может совмещать функции менеджера
    public static bool IsStaff => CurrentUser?.Role is UserRole.Admin or UserRole.Manager;
}