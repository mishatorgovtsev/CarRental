using CarRental.App.Data;
using CarRental.App.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRental.App.Services;

// Ручная регистрация платежа (окно «Платежи»). Платежи при выдаче и возврате создаёт RentalService
public class PaymentService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public PaymentService(AppDbContext db)
    {
        _db = db;
        _audit = new AuditService(db);
    }

    public Payment RegisterPayment(int contractId, PaymentType type, PaymentMethod method,
                                   decimal amount, string? comment)
    {
        var user = Session.CurrentUser;
        if (user == null || !Session.IsStaff)
            throw new BusinessException("Регистрировать платежи могут менеджер и администратор");
        if (amount <= 0)
            throw new BusinessException("Сумма должна быть больше нуля");

        var contract = _db.RentalContracts.Find(contractId)
                       ?? throw new BusinessException("Договор не найден");

        var payment = new Payment
        {
            ContractId = contractId,
            UserId = user.Id,
            Amount = amount,
            Method = method,
            Type = type,
            PaidAt = DateTime.Now,
            Comment = comment
        };
        _db.Payments.Add(payment);
        _db.SaveChanges();

        _audit.Log("Регистрация платежа", $"Договор {contract.ContractNumber}, {type}, {amount} ₽");
        return payment;
    }

    public List<Payment> GetPayments()
        => _db.Payments.Include(p => p.Contract)
              .OrderByDescending(p => p.PaidAt)
              .ToList();
}