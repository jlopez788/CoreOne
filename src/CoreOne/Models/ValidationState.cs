using System.Diagnostics;

namespace CoreOne.Models;

[DebuggerDisplay("{State}")]
public class ValidationState
{
    public static ValidationState Checking { get; } = new(ValidationStateType.Checking);
    public static ValidationState Valid { get; } = new(ValidationStateType.Valid);
    public string? ErrorMessage { get; }
    public ValidationStateType State { get; }

    public ValidationState(string? errorMessage)
    {
        ErrorMessage = errorMessage;
        State = errorMessage.IsNullOrEmpty() ? ValidationStateType.Valid : ValidationStateType.Invalid;
    }

    public ValidationState(ValidationStateType state, string? errorMessage = null)
    {
        State = state;
        ErrorMessage = errorMessage;
    }

    public static implicit operator bool(ValidationState? state) => state is null || state.State == ValidationStateType.Valid;

    public static implicit operator ValidationResult?(ValidationState state) => !string.IsNullOrEmpty(state?.ErrorMessage) ? new ValidationResult(state!.ErrorMessage) : ValidationResult.Success;

    public static bool operator !=(ValidationState? left, ValidationState? right) => !(left == right);

    public static bool operator ==(ValidationState? left, ValidationState? right) => left is null ? right is null : left.Equals(right);

    public override bool Equals(object? obj) => (obj is null && State == ValidationStateType.Valid) || (obj is ValidationState other && State == other.State && ErrorMessage.Matches(other.ErrorMessage));

    public override int GetHashCode() => (State, ErrorMessage).GetHashCode();
}