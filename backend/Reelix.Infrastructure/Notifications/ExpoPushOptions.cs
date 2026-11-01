namespace Reelix.Infrastructure.Notifications;

public sealed class ExpoPushOptions : Reelix.Application.Notifications.IPushRegistrationConfiguration
{
    public const string SectionName = "ExpoPush";

    public bool Enabled { get; set; }

    public Guid? ProjectId { get; set; }

    public string? AccessToken { get; set; }

    public int MaxAttempts { get; set; } = 5;

    bool Reelix.Application.Notifications.IPushRegistrationConfiguration.IsEnabled => Enabled;

    Guid? Reelix.Application.Notifications.IPushRegistrationConfiguration.ProjectId => ProjectId;

    int Reelix.Application.Notifications.IPushRegistrationConfiguration.MaxAttempts => MaxAttempts;
}
