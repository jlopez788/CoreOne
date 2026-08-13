using System.ComponentModel;

namespace CoreOne.Models.EventModels;

public class FieldChangingEventArgs<T>(T? current, T? next) : CancelEventArgs
{
    public T? CurrentValue { get; } = current;
    public T? NextValue { get; } = next;
}