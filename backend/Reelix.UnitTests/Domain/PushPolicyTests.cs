using Reelix.Domain.Notifications;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

/// <summary>The timing and eligibility rules of the push delivery, with no database (spec 0014, R3 and R5).</summary>
public sealed class PushPolicyTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(3, 20)]
    [InlineData(4, 40)]
    [InlineData(10, 2560)]
    [InlineData(11, 3600)]
    [InlineData(30, 3600)]
    public void TheSendBackoffDoublesFromFiveSecondsAndStopsAtAnHour(int attempt, double expectedSeconds)
    {
        var middle = PushRetryPolicy.NextSendDelay(attempt, null, 0.5);

        Assert.Equal(Math.Min(expectedSeconds, 3600), middle.TotalSeconds, precision: 6);
    }

    [Theory]
    [InlineData(0.0, 0.8)]
    [InlineData(0.5, 1.0)]
    [InlineData(0.999999, 1.2)]
    public void TheJitterSpreadsTheDelayByTwentyPercentEachWay(double jitter, double factor)
    {
        var delay = PushRetryPolicy.NextSendDelay(3, null, jitter);

        Assert.Equal(20 * factor, delay.TotalSeconds, precision: 3);
    }

    [Fact]
    public void TheProviderHintReplacesTheBackoffWhateverTheJitter()
    {
        Assert.Equal(
            TimeSpan.FromSeconds(120),
            PushRetryPolicy.NextSendDelay(5, TimeSpan.FromSeconds(120), 0.9));
    }

    [Theory]
    [InlineData(true, true, 5, 1)]
    [InlineData(false, false, 5, 1)]
    [InlineData(true, false, 5, 1)]
    [InlineData(false, true, 5, 5)]
    [InlineData(false, true, 3, 3)]
    public void ADeviceThatIsGoneOrAnErrorThatCannotBeRetriedGetsOneAttempt(
        bool deviceNotRegistered,
        bool retryable,
        int configured,
        int expected)
    {
        Assert.Equal(
            expected,
            PushRetryPolicy.MaxAttemptsAfterFailure(deviceNotRegistered, retryable, configured));
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(4, 240)]
    [InlineData(5, 360)]
    [InlineData(6, 360)]
    [InlineData(40, 360)]
    public void TheReceiptCheckBackoffDoublesFromFifteenMinutesAndStopsAtSixHours(int checks, double minutes)
    {
        Assert.Equal(TimeSpan.FromMinutes(minutes), PushRetryPolicy.ReceiptCheckDelay(checks));
    }

    [Fact]
    public void TheNamedDurationsKeepTheirValues()
    {
        Assert.Equal(TimeSpan.FromMinutes(2), PushRetryPolicy.LeaseDuration);
        Assert.Equal(TimeSpan.FromMinutes(15), PushRetryPolicy.FirstReceiptCheckDelay);
        Assert.Equal(TimeSpan.FromHours(24), PushRetryPolicy.ReceiptExpiry);
    }

    [Theory]
    [InlineData(-23, false)]
    [InlineData(-24, true)]
    [InlineData(-30, true)]
    public void ATicketExpiresAfterTwentyFourHours(int hoursAgo, bool expired)
    {
        Assert.Equal(expired, PushRetryPolicy.IsReceiptExpired(Now.AddHours(hoursAgo), Now));
    }

    [Fact]
    public void ATicketWithoutACreationDateDoesNotExpire()
    {
        Assert.False(PushRetryPolicy.IsReceiptExpired(null, Now));
    }

    private static UserNotification Notification(Guid accountId) =>
        UserNotification.Create(accountId, NotificationCategory.Social, "k", "t", "b", "/social", Now);

    private static NotificationDevice Device(Guid accountId, bool active = true)
    {
        var device = NotificationDevice.Create(
            accountId,
            Guid.NewGuid(),
            $"ExponentPushToken[{Guid.NewGuid():N}]",
            NotificationDevicePlatform.Ios,
            Guid.NewGuid(),
            Now);
        if (!active)
        {
            device.Deactivate();
        }

        return device;
    }

    private static NotificationPreferences Preferences(Guid accountId, bool social)
    {
        var preferences = NotificationPreferences.Create(accountId);
        preferences.Update(social, false, false);
        return preferences;
    }

    [Fact]
    public void ADeliveryWithEverythingInPlace_IsEligible()
    {
        var account = Guid.NewGuid();

        Assert.Null(PushDeliveryEligibility.CancellationReason(
            Notification(account),
            Device(account),
            Preferences(account, social: true)));
    }

    [Fact]
    public void AMissingNotificationOrDevice_CancelsAsInactive()
    {
        var account = Guid.NewGuid();

        Assert.Equal(
            "device_or_notification_inactive",
            PushDeliveryEligibility.CancellationReason(null, Device(account), Preferences(account, true)));
        Assert.Equal(
            "device_or_notification_inactive",
            PushDeliveryEligibility.CancellationReason(Notification(account), null, Preferences(account, true)));
    }

    [Fact]
    public void AnInactiveDeviceOrADeviceOfAnotherAccount_CancelsAsInactive()
    {
        var account = Guid.NewGuid();

        Assert.Equal(
            "device_or_notification_inactive",
            PushDeliveryEligibility.CancellationReason(
                Notification(account),
                Device(account, active: false),
                Preferences(account, true)));
        Assert.Equal(
            "device_or_notification_inactive",
            PushDeliveryEligibility.CancellationReason(
                Notification(account),
                Device(Guid.NewGuid()),
                Preferences(account, true)));
    }

    [Fact]
    public void MissingOrDisabledPreferences_CancelAsPreferenceDisabled()
    {
        var account = Guid.NewGuid();

        Assert.Equal(
            "preference_disabled",
            PushDeliveryEligibility.CancellationReason(Notification(account), Device(account), null));
        Assert.Equal(
            "preference_disabled",
            PushDeliveryEligibility.CancellationReason(
                Notification(account),
                Device(account),
                Preferences(account, social: false)));
    }
}
