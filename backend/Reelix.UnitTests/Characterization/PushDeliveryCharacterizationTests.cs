using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Notifications;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Batch 0 of spec 0014 (R3 and R9): the push delivery cycle (lease, cancellation, ticket, retry,
/// backoff, receipts) against PostgreSQL and a fake Expo server, with the clock under the test's
/// control. The jitter of the backoff is random, so those assertions use its +-20 % range.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class PushDeliveryCharacterizationTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    private const string TicketOk = """{"data":[{"status":"ok","id":"ticket-1"}]}""";

    private IServiceProvider Services => Fixture.PushEnabled.Services;

    private async Task<(TestAccount Account, NotificationDevice Device, NotificationDelivery Delivery)> SeedAsync(
        bool socialEnabled = true,
        bool deviceActive = true)
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SetPreferencesAsync(account.AccountId, social: socialEnabled);
        var device = await Fixture.AddDeviceAsync(account.AccountId, deviceActive);
        var delivery = await Fixture.AddPendingDeliveryAsync(account.AccountId, device.Id);
        return (account, device, delivery);
    }

    [Fact]
    public async Task ASuccessfulSend_LeasesForTwoMinutesAndStoresTheTicketWithAReceiptCheckInFifteenMinutes()
    {
        var (_, device, delivery) = await SeedAsync();
        NotificationDelivery? duringSend = null;
        Fixture.Expo.Responder = (_, _) =>
        {
            using var dbContext = Fixture.CreateDbContext();
            duringSend = dbContext.NotificationDeliveries.Single(item => item.Id == delivery.Id);
            return FakeExpoHandler.Json(TicketOk);
        };

        var processed = await WorkerHarness.DeliverPushAsync(Services);

        Assert.Equal(1, processed);
        Assert.NotNull(duringSend);
        Assert.Equal(NotificationDeliveryStatus.Pending, duringSend.Status);
        Assert.Equal(1, duringSend.AttemptCount);
        Assert.NotNull(duringSend.LeaseId);
        Assert.Equal(Clock.GetUtcNow().AddMinutes(2), duringSend.LeaseUntil);
        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, stored.Status);
        Assert.Equal("ticket-1", stored.ExpoTicketId);
        Assert.Equal(Clock.GetUtcNow(), stored.ExpoTicketCreatedAt);
        Assert.Equal(Clock.GetUtcNow().AddMinutes(15), stored.ReceiptCheckAt);
        Assert.Equal(1, stored.AttemptCount);
        Assert.Equal(0, stored.ReceiptCheckCount);
        Assert.Null(stored.LeaseId);
        Assert.Null(stored.LeaseUntil);
        var (path, body) = Assert.Single(Fixture.Expo.Requests);
        Assert.Equal("/--/api/v2/push/send", path);
        using var sent = JsonDocument.Parse(body);
        Assert.Equal(device.ExpoPushToken, sent.RootElement.GetProperty("to").GetString());
        Assert.Equal("Título", sent.RootElement.GetProperty("title").GetString());
        Assert.Equal("Corpo da notificação.", sent.RootElement.GetProperty("body").GetString());
        Assert.Equal("/social", sent.RootElement.GetProperty("data").GetProperty("deepLink").GetString());
        Assert.Equal("default", sent.RootElement.GetProperty("sound").GetString());
        Assert.Equal("high", sent.RootElement.GetProperty("priority").GetString());
        Assert.Equal("default", sent.RootElement.GetProperty("channelId").GetString());
    }

    [Fact]
    public async Task ADeliveryWhoseCategoryWasTurnedOff_IsCancelledWithoutCallingExpo()
    {
        var (_, _, delivery) = await SeedAsync(socialEnabled: false);

        var processed = await WorkerHarness.DeliverPushAsync(Services);

        Assert.Equal(0, processed);
        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, stored.Status);
        Assert.Equal("preference_disabled", stored.LastErrorCode);
        Assert.Equal(Clock.GetUtcNow(), stored.CompletedAt);
        Assert.Empty(Fixture.Expo.Requests);
    }

    [Fact]
    public async Task ADeliveryOfAnInactiveDevice_IsCancelledWithoutCallingExpo()
    {
        var (_, _, delivery) = await SeedAsync(deviceActive: false);

        var processed = await WorkerHarness.DeliverPushAsync(Services);

        Assert.Equal(0, processed);
        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, stored.Status);
        Assert.Equal("device_or_notification_inactive", stored.LastErrorCode);
        Assert.Empty(Fixture.Expo.Requests);
    }

    [Fact]
    public async Task WhenPushIsDisabled_TheCycleDoesNothing()
    {
        var (_, _, delivery) = await SeedAsync();

        var processed = await WorkerHarness.DeliverPushAsync(Fixture.PushDisabled.Services);

        Assert.Equal(0, processed);
        Assert.Equal(NotificationDeliveryStatus.Pending, (await Fixture.ReadDeliveryAsync(delivery.Id)).Status);
        Assert.Empty(Fixture.Expo.Requests);
    }

    [Fact]
    public async Task ARetryableFailure_BacksOffExponentiallyWithJitterAndFailsAtTheAttemptLimit()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError);

        // Delays are 5 * 2^(attempt - 1) seconds with a jitter of +-20 %.
        var expectedBaseSeconds = new[] { 5, 10, 20, 40 };
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var before = Clock.GetUtcNow();
            Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));
            var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
            Assert.Equal(NotificationDeliveryStatus.Pending, stored.Status);
            Assert.Equal(attempt, stored.AttemptCount);
            Assert.Equal("http_500", stored.LastErrorCode);
            Assert.Null(stored.LeaseId);
            var delay = (stored.NextAttemptAt - before).TotalSeconds;
            Assert.InRange(delay, expectedBaseSeconds[attempt - 1] * 0.8 - 0.01, expectedBaseSeconds[attempt - 1] * 1.2 + 0.01);
            Clock.Advance(TimeSpan.FromMinutes(5));
        }

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));
        var failed = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Failed, failed.Status);
        Assert.Equal(5, failed.AttemptCount);
        Assert.Equal("http_500", failed.LastErrorCode);
        Assert.Equal(Clock.GetUtcNow(), failed.CompletedAt);
    }

    [Theory]
    [InlineData("120", 120)]
    [InlineData("0", 1)]
    [InlineData("99999", 3600)]
    [InlineData(null, 60)]
    public async Task TooManyRequests_HonoursRetryAfterClampedToAnHourOrWaitsAMinute(
        string? retryAfter,
        int expectedSeconds)
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            if (retryAfter is not null)
            {
                response.Headers.RetryAfter = new RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(int.Parse(retryAfter, System.Globalization.CultureInfo.InvariantCulture)));
            }

            return response;
        };

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Pending, stored.Status);
        Assert.Equal("http_429", stored.LastErrorCode);
        Assert.Equal(Clock.GetUtcNow().AddSeconds(expectedSeconds), stored.NextAttemptAt);
    }

    [Fact]
    public async Task ANonRetryableTicketError_FailsAfterASingleAttemptAndKeepsTheDevice()
    {
        var (_, device, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(
            """{"data":[{"status":"error","message":"bad","details":{"error":"InvalidCredentials"}}]}""");

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.Status);
        Assert.Equal(1, stored.AttemptCount);
        Assert.Equal("InvalidCredentials", stored.LastErrorCode);
        Assert.True((await Fixture.ReadDeviceAsync(device.Id)).IsActive);
    }

    [Fact]
    public async Task DeviceNotRegistered_FailsAfterASingleAttemptAndDeactivatesTheDevice()
    {
        var (_, device, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(
            """{"data":[{"status":"error","message":"gone","details":{"error":"DeviceNotRegistered"}}]}""");

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.Status);
        Assert.Equal(1, stored.AttemptCount);
        Assert.Equal("DeviceNotRegistered", stored.LastErrorCode);
        Assert.False((await Fixture.ReadDeviceAsync(device.Id)).IsActive);
    }

    [Fact]
    public async Task ARetryableTicketError_KeepsTheDeliveryPendingWithTheExponentialDelay()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(
            """{"data":[{"status":"error","message":"slow down","details":{"error":"MessageRateExceeded"}}]}""");

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Pending, stored.Status);
        Assert.Equal("MessageRateExceeded", stored.LastErrorCode);
        Assert.InRange((stored.NextAttemptAt - Clock.GetUtcNow()).TotalSeconds, 3.99, 6.01);
    }

    [Theory]
    [InlineData("transport", "expo_transport_error")]
    [InlineData("timeout", "expo_timeout")]
    [InlineData("json", "expo_transport_error")]
    public async Task ATransportFailure_IsRecordedAsARetryableErrorWithTheSameCodeAsBefore(
        string failure,
        string expectedCode)
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => failure switch
        {
            "transport" => throw new HttpRequestException("connection refused"),
            "timeout" => throw new TaskCanceledException("timed out"),
            _ => FakeExpoHandler.Json("this is not json"),
        };

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Pending, stored.Status);
        Assert.Equal(expectedCode, stored.LastErrorCode);
        Assert.Equal(1, stored.AttemptCount);
        Assert.InRange((stored.NextAttemptAt - Clock.GetUtcNow()).TotalSeconds, 3.99, 6.01);
    }

    [Fact]
    public async Task AFailureOfOneItem_RecordsTheRetryOnlyForItAndTheOthersProceed()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();
        Fixture.Expo.Responder = (_, body) => body.Contains(first.Device.ExpoPushToken, StringComparison.Ordinal)
            ? throw new HttpRequestException("boom")
            : FakeExpoHandler.Json(TicketOk);

        Assert.Equal(2, await WorkerHarness.DeliverPushAsync(Services));

        Assert.Equal(NotificationDeliveryStatus.Pending, (await Fixture.ReadDeliveryAsync(first.Delivery.Id)).Status);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, (await Fixture.ReadDeliveryAsync(second.Delivery.Id)).Status);
    }

    [Fact]
    public async Task AReadyReceipt_CompletesTheDelivery()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromMinutes(16));
        Fixture.Expo.Responder = (request, _) => request.RequestUri!.AbsolutePath.EndsWith("getReceipts", StringComparison.Ordinal)
            ? FakeExpoHandler.Json("""{"data":{"ticket-1":{"status":"ok"}}}""")
            : new HttpResponseMessage(HttpStatusCode.InternalServerError);

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Completed, stored.Status);
        Assert.Equal(Clock.GetUtcNow(), stored.CompletedAt);
        Assert.Null(stored.ReceiptCheckAt);
        Assert.Null(stored.LastErrorCode);
    }

    [Fact]
    public async Task ANotReadyReceipt_IsRescheduledWithTheReceiptBackoff()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromMinutes(16));
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json("""{"data":{}}""");

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        // The claim reschedules by 15 minutes (check 0 -> 1); the not ready answer reschedules
        // again by 15 * 2^1 minutes (check 1 -> 2). Characterized as is.
        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, stored.Status);
        Assert.Equal(2, stored.ReceiptCheckCount);
        Assert.Equal(Clock.GetUtcNow().AddMinutes(30), stored.ReceiptCheckAt);
        Assert.Equal("receipt_not_ready", stored.LastErrorCode);
    }

    [Fact]
    public async Task AReceiptCheckThatReturnsAnHttpError_IsRescheduledWithTheHttpCode()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromMinutes(16));
        Fixture.Expo.Responder = (_, _) => new HttpResponseMessage(HttpStatusCode.BadGateway);

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, stored.Status);
        Assert.Equal("http_502", stored.LastErrorCode);
    }

    [Fact]
    public async Task AReceiptWithDeviceNotRegistered_FailsTheDeliveryAndDeactivatesTheDevice()
    {
        var (_, device, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromMinutes(16));
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(
            """{"data":{"ticket-1":{"status":"error","message":"gone","details":{"error":"DeviceNotRegistered"}}}}""");

        Assert.Equal(1, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.Status);
        Assert.Equal("DeviceNotRegistered", stored.LastErrorCode);
        Assert.False((await Fixture.ReadDeviceAsync(device.Id)).IsActive);
    }

    [Fact]
    public async Task ATicketOlderThanTwentyFourHours_ExpiresWithoutAskingExpo()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromHours(25));
        Fixture.Expo.Reset();

        Assert.Equal(0, await WorkerHarness.DeliverPushAsync(Services));

        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Failed, stored.Status);
        Assert.Equal("receipt_expired", stored.LastErrorCode);
        Assert.Equal(Clock.GetUtcNow(), stored.CompletedAt);
        Assert.Empty(Fixture.Expo.Requests);
    }

    [Fact]
    public async Task ATransportFailureWhileReadingAReceipt_AbortsTheBatchAndKeepsTheClaim()
    {
        var (_, _, delivery) = await SeedAsync();
        Fixture.Expo.Responder = (_, _) => FakeExpoHandler.Json(TicketOk);
        await WorkerHarness.DeliverPushAsync(Services);
        Clock.Advance(TimeSpan.FromMinutes(16));
        Fixture.Expo.Responder = (_, _) => throw new HttpRequestException("connection refused");

        await Assert.ThrowsAsync<HttpRequestException>(() => WorkerHarness.DeliverPushAsync(Services));

        // The claim was committed before the receipt call: the next check is already rescheduled.
        var stored = await Fixture.ReadDeliveryAsync(delivery.Id);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, stored.Status);
        Assert.Equal(1, stored.ReceiptCheckCount);
        Assert.Equal(Clock.GetUtcNow().AddMinutes(15), stored.ReceiptCheckAt);
    }

    [Fact]
    public async Task UpdatingPreferences_CancelsThePendingDeliveriesOfTheCategoryTurnedOff()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SetPreferencesAsync(account.AccountId, social: true, newEpisode: true, premiere: true);
        var device = await Fixture.AddDeviceAsync(account.AccountId);
        var social = await Fixture.AddPendingDeliveryAsync(account.AccountId, device.Id, NotificationCategory.Social);
        var newEpisode = await Fixture.AddPendingDeliveryAsync(account.AccountId, device.Id, NotificationCategory.NewEpisode);

        var response = await PutAsync(
            account.Subject,
            "/api/v1/notifications/preferences",
            new { socialEnabled = false, newEpisodeEnabled = true, premiereEnabled = true });

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var cancelled = await Fixture.ReadDeliveryAsync(social.Id);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, cancelled.Status);
        Assert.Equal("preference_disabled", cancelled.LastErrorCode);
        Assert.Equal(Clock.GetUtcNow(), cancelled.CompletedAt);
        Assert.Equal(NotificationDeliveryStatus.Pending, (await Fixture.ReadDeliveryAsync(newEpisode.Id)).Status);
    }
}
