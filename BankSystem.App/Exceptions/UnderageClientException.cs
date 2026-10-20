namespace BankSystem.App.Services;

// Собственное исключение из практики по обработке ошибок.
public sealed class UnderageClientException : Exception
{
    public UnderageClientException(string message) : base(message)
    {
    }
}
