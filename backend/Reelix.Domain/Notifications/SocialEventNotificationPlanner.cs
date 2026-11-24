using Reelix.Domain.Social;

namespace Reelix.Domain.Notifications;

/// <summary>How a social event was resolved. The persisted text is part of the data contract.</summary>
public enum SocialEventOutcome
{
    /// <summary>A notification (and one delivery per active device) was created.</summary>
    Created,

    /// <summary>The recipient does not want social notifications.</summary>
    Disabled,

    /// <summary>The event no longer matches the relationship, or its type is unknown.</summary>
    Obsolete,
}

public static class SocialEventOutcomes
{
    /// <summary>The text stored in <c>notification_processed_social_events.outcome</c>.</summary>
    public static string ToPersistedText(this SocialEventOutcome outcome) =>
        outcome switch
        {
            SocialEventOutcome.Created => "created",
            SocialEventOutcome.Disabled => "disabled",
            SocialEventOutcome.Obsolete => "obsolete",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
}

/// <summary>The decision for one social event.</summary>
public sealed record SocialEventPlan(
    SocialEventOutcome Outcome,
    UserNotification? Notification,
    IReadOnlyList<NotificationDelivery> Deliveries)
{
    public static SocialEventPlan WithoutNotification(SocialEventOutcome outcome) =>
        new(outcome, null, []);
}

/// <summary>
/// The pure rule that turns a social event into a notification: the event must still match the
/// relationship, the recipient must want social notifications, and the notification is delivered
/// to every active device of the recipient.
/// </summary>
public static class SocialEventNotificationPlanner
{
    private const string DeepLink = "/social";

    /// <param name="socialEvent">The event taken from the outbox.</param>
    /// <param name="relationship">The relationship between actor and recipient, if there is one.</param>
    /// <param name="preferences">The notification preferences of the recipient, if they saved any.</param>
    /// <param name="recipientDevices">The devices registered by the recipient (inactive ones are ignored).</param>
    /// <param name="now">The instant stamped on the notification and its deliveries.</param>
    public static SocialEventPlan Plan(
        SocialEvent socialEvent,
        FriendRelationship? relationship,
        NotificationPreferences? preferences,
        IReadOnlyCollection<NotificationDevice> recipientDevices,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(socialEvent);
        ArgumentNullException.ThrowIfNull(recipientDevices);

        var expectedStatus = ExpectedRelationshipStatus(socialEvent.EventType);
        if (expectedStatus is null ||
            relationship is null ||
            relationship.Status != expectedStatus ||
            !IsBetweenActorAndRecipient(relationship, socialEvent) ||
            socialEvent.EventType == SocialEventType.FriendRequested &&
            (relationship.RequesterAccountId != socialEvent.ActorAccountId ||
             relationship.AddresseeAccountId != socialEvent.RecipientAccountId))
        {
            return SocialEventPlan.WithoutNotification(SocialEventOutcome.Obsolete);
        }

        if (preferences is null || !preferences.IsEnabled(NotificationCategory.Social))
        {
            return SocialEventPlan.WithoutNotification(SocialEventOutcome.Disabled);
        }

        var (title, body) = ContentOf(socialEvent.EventType);
        var notification = UserNotification.Create(
            socialEvent.RecipientAccountId,
            NotificationCategory.Social,
            $"social:{socialEvent.Id}",
            title,
            body,
            DeepLink,
            now);
        var deliveries = recipientDevices
            .Where(device => device.IsActive && device.AccountId == socialEvent.RecipientAccountId)
            .Select(device => NotificationDelivery.Create(notification.Id, device.Id, now))
            .ToArray();
        return new SocialEventPlan(SocialEventOutcome.Created, notification, deliveries);
    }

    private static FriendRelationshipStatus? ExpectedRelationshipStatus(string eventType) =>
        eventType switch
        {
            SocialEventType.FriendRequested => FriendRelationshipStatus.Pending,
            SocialEventType.FriendAccepted => FriendRelationshipStatus.Accepted,
            _ => null,
        };

    private static bool IsBetweenActorAndRecipient(FriendRelationship relationship, SocialEvent socialEvent) =>
        relationship.Includes(socialEvent.ActorAccountId) &&
        relationship.Includes(socialEvent.RecipientAccountId);

    private static (string Title, string Body) ContentOf(string eventType) =>
        eventType switch
        {
            SocialEventType.FriendRequested => (
                "Nova solicitação de amizade",
                "Você recebeu uma solicitação de amizade."),
            SocialEventType.FriendAccepted => (
                "Solicitação aceita",
                "Sua solicitação de amizade foi aceita."),
            _ => throw new InvalidOperationException("Tipo de evento social inesperado."),
        };
}
