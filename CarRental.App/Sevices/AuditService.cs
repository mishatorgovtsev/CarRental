using CarRental.App.Data;
using CarRental.App.Models;

namespace CarRental.App.Services;

public class AuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    // Записывает действие в журнал. Если userId не передан, берётся текущий пользователь
    public void Log(string action, string? details = null, int? userId = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId ?? Session.CurrentUser?.Id,
            At = DateTime.Now,
            Action = action,
            Details = details
        });
        _db.SaveChanges();
    }
}