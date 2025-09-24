using CoreOne.Hubs;
using CoreOne.Identity.Models;
using System.ComponentModel;

namespace CoreOne.Identity.Events;

public class UserChangingEventArgs(UserIdentity? user) : CancelEventArgs, IHubMessage
{
    public bool IsAuthenticated => User?.IsAuthenticated == true;
    public bool IsImpersonating => User?.IsImpersonating == true;
    public UserIdentity? User { get; set; } = user;
    public string? Username => User?.Username;
}