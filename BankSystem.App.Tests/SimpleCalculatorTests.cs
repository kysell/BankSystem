using ConsoleApp4;
using NUnit.Framework;

namespace BankSystem.App.Tests;

public class SimpleCalculatorTests
{
    [TestCase(2, 5, 7)]
    [TestCase(-2, 2, 0)]
    public void Calculator_Sums(int a, int b, int expected) => Assert.That(new SimpleCalculator().GetSum(a, b), Is.EqualTo(expected));
    [Test]
    public void Calculator_OperationsAndErrors()
    {
        var calculator = new SimpleCalculator();
        Assert.That(calculator.GetSubtraction(5, 4), Is.EqualTo(1));
        Assert.That(calculator.GetMultiplication(2, 3), Is.EqualTo(6));
        Assert.That(calculator.GetDivision(7, 2), Is.EqualTo(3));
        Assert.Throws<DivideByZeroException>(() => calculator.GetDivision(1, 0));
        Assert.Throws<OverflowException>(() => calculator.GetSum(int.MaxValue, 1));
    }
}
