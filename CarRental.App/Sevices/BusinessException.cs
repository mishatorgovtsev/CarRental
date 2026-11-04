namespace CarRental.App.Services;

// Ошибка бизнес-логики: её текст показываем пользователю в MessageBox
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}