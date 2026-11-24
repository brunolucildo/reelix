namespace Reelix.Application.Notifications;

/// <param name="Processed">How many events the cycle handled; the worker waits less when there was work.</param>
public sealed record ProcessSocialEventsResult(int Processed);
