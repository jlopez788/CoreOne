namespace CoreOne.Hubs;

public record ExceptionMessage(Exception Exception, string Message) : IHubMessage;