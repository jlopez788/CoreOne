using System.ComponentModel;

namespace CoreOne.Models.EventArgs;

public class BackingFieldChangingEventArgs<T>(T? current, T? next) : CancelEventArgs
{
    public T? CurrentValue { get; } = current;
    public T? NextValue { get; } = next;
}
