namespace CoreOne.Models.EventModels;

public class FieldChangedEventArgs<T>(T? currentValue) : EventArgs
{
    public T? CurrentValue { get; } = currentValue;
}