namespace ConsoleApp4;

internal static class Program
{
    private static void Main()
    {
        var calculator = new SimpleCalculator();
        Console.WriteLine($"2 + 5 = {calculator.GetSum(2, 5)}");
        Console.WriteLine($"5 - 4 = {calculator.GetSubtraction(5, 4)}");
        Console.WriteLine($"2 * 3 = {calculator.GetMultiplication(2, 3)}");
        Console.WriteLine($"8 / 2 = {calculator.GetDivision(8, 2)}");
    }
}
