using System.Reflection;
using System.Text.RegularExpressions;

namespace BankSystem.App.Services;

public static class DelegateExamples
{
    public static int Apply(Func<int, int> operation, int value) => operation(value);
    public static IReadOnlyList<int> Filter(IEnumerable<int> values, Predicate<int> predicate)
    {
        var result = new List<int>();
        foreach (int value in values)
        {
            if (predicate(value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    public static void InvokeThenRemove(Action action, Action handler)
    {
        Action? combined = action + handler;
        combined -= handler;
        combined?.Invoke();
    }
}

public static class StringExtensions
{
    public static TimeSpan Seconds(this int value) => TimeSpan.FromSeconds(value);
}

public static class ReflectionTools
{
    public static object Create(string typeName)
    {
        Type? type = Type.GetType(typeName);
        if (type == null)
        {
            throw new ArgumentException("Type not found.", nameof(typeName));
        }

        object? instance = Activator.CreateInstance(type);
        if (instance == null)
        {
            throw new InvalidOperationException($"Cannot create {type.FullName}.");
        }

        return instance;
    }

    public static object? Invoke(object target, string methodName, params object? [] parameters)
    {
        Type type = target.GetType();
        MethodInfo? method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            throw new MissingMethodException(type.FullName, methodName);
        }

        // null здесь допустим: например, у метода с возвращаемым типом void.
        return method.Invoke(target, parameters);
    }

    public static object? GetPrivateProperty(object target, string propertyName)
    {
        Type type = target.GetType();
        PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null)
        {
            throw new MissingMemberException(type.FullName, propertyName);
        }

        return property.GetValue(target);
    }
}

public static class TextPatterns
{
    public static bool IsSocialSecurityNumber(string value)
    {
        return Regex.IsMatch(value, @"^\d{3}-\d{2}-\d{4}$");
    }
    public static IReadOnlyList<string> FindDuplicateWords(string value)
    {
        MatchCollection matches = Regex.Matches(value, @"\b(\w+)\W+\1\b", RegexOptions.IgnoreCase);
        var words = new List<string>();
        foreach (Match match in matches)
        {
            words.Add(match.Groups[1].Value);
        }

        return words;
    }

    public static string ReplaceMoney(string value)
    {
        return Regex.Replace(value, @"\b\d+\.\d{2}\b", "$$$&");
    }
}
