namespace ConsoleApp4;

// Самостоятельное задание из раздела «Модульное тестирование».
public sealed class SimpleCalculator
{
    public int GetSum(int first, int second)
    {
        return checked(first + second);
    }

    public int GetSubtraction(int first, int second)
    {
        return checked(first - second);
    }

    public int GetMultiplication(int first, int second)
    {
        return checked(first * second);
    }

    public int GetDivision(int first, int second)
    {
        return first / second;
    }
}
