using CoreOne.Hubs;
using CoreOne.Identity.Models;

namespace CoreOne.Identity.Events;

public class UserChangedEventArgs(UserIdentity? user) : EventArgs, IHubMessage
{
    public bool IsAuthenticated => User?.IsAuthenticated == true;
    public bool IsImpersonating => User?.IsImpersonating == true;
    public UserIdentity? User { get; init; } = user;
    public string? Username => User?.Username;
}