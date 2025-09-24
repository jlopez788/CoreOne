using CoreOne.Identity.Events;
using CoreOne.Identity.Models;

namespace CoreOne.Identity.Contracts;

public interface ICurrentUser : ICurrentUsername
{
    BearerToken BearerToken { get; }
    bool IsAuthenticated => User?.IsAuthenticated == true && !IsExpired;
    bool IsExpired { get; }
    bool IsImpersonating => User?.IsImpersonating == true;
    UserIdentity? User { get; }

    Task Initialize();

    Task RemoveImpersonation();

    Task SetImpersonation(BearerToken token);

    void Subscribe(Func<UserChangedEventArgs, Task> onNext, CancellationToken cancellationToken);

    void Subscribe(Func<UserChangingEventArgs, Task> onNext, CancellationToken cancellationToken);
}