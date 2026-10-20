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

    public static int InvokeThenRemove(Action action, Action handler)
    {
        Action? combined = action + handler;
        combined -= handler;
        combined?.Invoke();
        return 1;
    }
}

public static class StringExtensions
{
    public static int WordCount(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        char[] separators =
        {
            ' ',
            '.',
            '?',
            '!'
        };
        string[] words = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    public static TimeSpan Seconds(this int value) => TimeSpan.FromSeconds(value);
}

public static class ReflectionTools
{
    public static string PrintObjectProperties(object target)
    {
        ArgumentNullException.ThrowIfNull(target);
        Type type = target.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var descriptions = new List<string>();
        foreach (PropertyInfo property in properties)
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value = property.GetValue(target);
            descriptions.Add($"{property.Name}: {value}");
        }

        return string.Join(", ", descriptions);
    }

    public static object Create(string typeName)
    {
        Type? type = Type.GetType(typeName);
        if (type == null)
        {
            throw new ArgumentException("Type not found.", nameof(typeName));
        }

        return Create(type);
    }

    public static object Create(Type type)
    {
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

public static partial class TextPatterns
{
    [GeneratedRegex(@"^\d{3}-\d{2}-\d{4}$")]
    private static partial Regex SsnRegex();
    public static bool IsSocialSecurityNumber(string value) => SsnRegex().IsMatch(value);
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
