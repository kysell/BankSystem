using System.ComponentModel;

namespace BankSystem.App.Services;

public sealed class ThresholdQueue<T>
{
    private readonly int _threshold;
    private readonly Queue<T> _items = new Queue<T>();
    public event EventHandler? ThresholdReached;
    public event EventHandler? BecameEmpty;
    public ThresholdQueue(int threshold)
    {
        if (threshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        _threshold = threshold;
    }

    public void Enqueue(T item)
    {
        _items.Enqueue(item);
        if (_items.Count == _threshold + 1)
        {
            ThresholdReached?.Invoke(this, EventArgs.Empty);
        }
    }

    public T Dequeue()
    {
        T item = _items.Dequeue();
        if (_items.Count == 0)
        {
            BecameEmpty?.Invoke(this, EventArgs.Empty);
        }

        return item;
    }
}

public sealed class NumberStreamAnalyzer
{
    private readonly decimal _allowedPercent;
    private decimal? _previous;
    public event EventHandler<NumberChangedEventArgs>? DifferenceExceeded;
    public NumberStreamAnalyzer(decimal allowedPercent)
    {
        if (allowedPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allowedPercent));
        }

        _allowedPercent = allowedPercent;
    }

    public void Add(decimal value)
    {
        if (_previous.HasValue)
        {
            decimal previousValue = _previous.Value;
            bool differenceExceeded;
            if (previousValue == 0)
            {
                // От нуля процент не считается: отдельно проверяем появление ненулевого значения.
                differenceExceeded = value != 0;
            }
            else
            {
                decimal differencePercent = Math.Abs((value - previousValue) / previousValue) * 100;
                differenceExceeded = differencePercent > _allowedPercent;
            }

            if (differenceExceeded)
            {
                DifferenceExceeded?.Invoke(this, new NumberChangedEventArgs(value));
            }
        }

        _previous = value;
    }
}

public sealed class NumberChangedEventArgs : EventArgs
{
    public decimal Value { get; }

    public NumberChangedEventArgs(decimal value)
    {
        Value = value;
    }
}

public sealed class ObservableBalance : INotifyPropertyChanged
{
    private decimal _amount;
    public event PropertyChangedEventHandler? PropertyChanged;
    public decimal Amount
    {
        get => _amount;
        set
        {
            if (_amount == value)
            {
                return;
            }

            _amount = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
        }
    }
}
