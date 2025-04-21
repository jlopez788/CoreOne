namespace CoreOne.Models.EventArgs;

public class BackingFieldChangedEventArgs<T>(T? currentValue)
{
    public T? CurrentValue { get; } = currentValue;
}