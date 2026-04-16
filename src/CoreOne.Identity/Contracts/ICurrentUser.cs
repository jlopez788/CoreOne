using CoreOne.Identity.Events;
using CoreOne.Identity.Models;

namespace CoreOne.Identity.Contracts;

public interface ICurrentUser<TUser> : ICurrentUsername where TUser : UserIdentity
{
    BearerToken BearerToken { get; }
    bool IsExpired { get; }
#if NET9_0_OR_GREATER
    bool IsAuthenticated => User?.IsAuthenticated == true && !IsExpired;
    bool IsImpersonating => User?.IsImpersonating == true;
#else
    bool IsAuthenticated { get; }
    bool IsImpersonating { get; }
#endif
    TUser? User { get; }

    Task Initialize();

    Task RemoveImpersonation();

    Task SetImpersonation(BearerToken token);

    void Subscribe(Func<UserChangedEventArgs, Task> onNext, CancellationToken cancellationToken);

    void Subscribe(Func<UserChangingEventArgs, Task> onNext, CancellationToken cancellationToken);
}

public interface ICurrentUser : ICurrentUser<UserIdentity>, ICurrentUsername
{
}