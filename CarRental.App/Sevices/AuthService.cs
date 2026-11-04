using CarRental.App.Data;
using CarRental.App.Models;

namespace CarRental.App.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly AuditService _audit;

    public AuthService(AppDbContext db)
    {
        _db = db;
        _audit = new AuditService(db);
    }

    // Вход: проверка логина и пароля, запись в журнал, запоминаем пользователя в Session
    public User Login(string login, string password)
    {
        login = (login ?? "").Trim();
        var user = _db.Users.FirstOrDefault(u => u.Login == login);

        // Одно и то же сообщение для всех случаев, чтобы не подсказывать, что именно неверно
        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password ?? "", user.PasswordHash))
            throw new BusinessException("Неверный логин или пароль");

        Session.CurrentUser = user;
        _audit.Log("Вход в систему", $"Логин: {user.Login}", user.Id);
        return user;
    }

    // Регистрация клиента: создаёт учётную запись (роль «клиент») и карточку клиента
    public User RegisterClient(string login, string password, string passwordRepeat, Client client)
    {
        login = (login ?? "").Trim();
        if (login.Length < 3)
            throw new BusinessException("Логин должен быть не короче 3 символов");
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            throw new BusinessException("Пароль должен быть не короче 6 символов");
        if (password != passwordRepeat)
            throw new BusinessException("Пароли не совпадают");

        var error = ValidateClient(client);
        if (error != null)
            throw new BusinessException(error);

        if (_db.Users.Any(u => u.Login == login))
            throw new BusinessException("Такой логин уже занят");

        var user = new User
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = $"{client.LastName} {client.FirstName} {client.MiddleName}".Trim(),
            Role = UserRole.Client,
            CreatedAt = DateTime.Now
        };
        client.User = user;
        _db.Clients.Add(client);   // пользователь сохранится вместе с клиентом
        _db.SaveChanges();

        _audit.Log("Регистрация клиента", $"Логин: {login}", user.Id);
        return user;
    }

    // Создание менеджера (только администратор)
    public User CreateManager(string login, string password, string fullName)
    {
        if (!Session.IsAdmin)
            throw new BusinessException("Создавать менеджеров может только администратор");

        login = (login ?? "").Trim();
        fullName = (fullName ?? "").Trim();
        if (login.Length < 3)
            throw new BusinessException("Логин должен быть не короче 3 символов");
        if (string.IsNullOrEmpty(password) || password.Length < 6)
            throw new BusinessException("Пароль должен быть не короче 6 символов");
        if (fullName.Length == 0)
            throw new BusinessException("Укажите ФИО менеджера");
        if (_db.Users.Any(u => u.Login == login))
            throw new BusinessException("Такой логин уже занят");

        var user = new User
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            FullName = fullName,
            Role = UserRole.Manager,
            CreatedAt = DateTime.Now
        };
        _db.Users.Add(user);
        _db.SaveChanges();

        _audit.Log("Создан менеджер", $"Логин: {login}");
        return user;
    }

    // Проверка данных клиента (пригодится и при редактировании клиента). Возвращает текст ошибки или null
    public static string? ValidateClient(Client c)
    {
        if (string.IsNullOrWhiteSpace(c.LastName)) return "Укажите фамилию";
        if (string.IsNullOrWhiteSpace(c.FirstName)) return "Укажите имя";
        if (string.IsNullOrWhiteSpace(c.Phone)) return "Укажите телефон";
        if (string.IsNullOrWhiteSpace(c.Email) || !c.Email.Contains('@')) return "Укажите корректный e-mail";
        if (c.BirthDate >= DateTime.Today) return "Дата рождения должна быть в прошлом";
        if (string.IsNullOrWhiteSpace(c.PassportSeriesNumber)) return "Укажите серию и номер паспорта";
        if (string.IsNullOrWhiteSpace(c.PassportIssuedBy)) return "Укажите, кем выдан паспорт";
        if (string.IsNullOrWhiteSpace(c.LicenseNumber)) return "Укажите номер водительского удостоверения";
        if (string.IsNullOrWhiteSpace(c.LicenseCategory)) return "Укажите категорию прав";
        if (c.LicenseExpiryDate <= c.LicenseIssueDate) return "Срок действия прав должен быть позже даты выдачи";
        return null;
    }
}