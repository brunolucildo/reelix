using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Notifications;

/// <summary>The two worker use cases with in-memory ports: no database, no Expo, no real clock (spec 0014, R2, R3 and R13).</summary>
public sealed class PushWorkerUseCaseTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;
    private static readonly Guid Account = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Friend = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    // ---- social events -------------------------------------------------------------------------

    private sealed class Outbox(params SocialEvent[] events) : ISocialEventOutbox, ISocialEventBatch
    {
        public int ReservedBatchSize { get; private set; }

        public List<(SocialEventPlan Plan, ProcessedNotificationEvent Receipt)> Staged { get; } = [];

        public bool Committed { get; private set; }

        public bool Disposed { get; private set; }

        public IReadOnlyList<SocialEvent> Events => events;

        public Task<ISocialEventBatch> ReserveAsync(int batchSize, CancellationToken cancellationToken)
        {
            ReservedBatchSize = batchSize;
            return Task.FromResult<ISocialEventBatch>(this);
        }

        public void Stage(SocialEventPlan plan, ProcessedNotificationEvent receipt) => Staged.Add((plan, receipt));

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class Friendships(FriendRelationship? relationship) : IFriendships
    {
        public Task<FriendRelationship?> FindBetweenAsync(Guid first, Guid second, CancellationToken cancellationToken) =>
            Task.FromResult(relationship);
    }

    private sealed class Preferences(NotificationPreferences? preferences) : INotificationPreferencesStore
    {
        public Task<NotificationPreferences?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult(preferences);

        public void Add(NotificationPreferences value) => throw new NotSupportedException();
    }

    private sealed class Devices(params NotificationDevice[] devices) : INotificationDeviceLookup
    {
        public Task<IReadOnlyList<NotificationDevice>> ListActiveAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationDevice>>(devices.Where(device => device.AccountId == accountId && device.IsActive).ToArray());

        public Task<NotificationDevice?> FindAsync(Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(devices.SingleOrDefault(device => device.Id == deviceId));
    }

    private static NotificationDevice NewDevice(Guid accountId) =>
        NotificationDevice.Create(
            accountId,
            Guid.NewGuid(),
            $"ExponentPushToken[{Guid.NewGuid():N}]",
            NotificationDevicePlatform.Android,
            Guid.NewGuid(),
            Now);

    private static NotificationPreferences SocialOn(Guid accountId)
    {
        var preferences = NotificationPreferences.Create(accountId);
        preferences.Update(true, false, false);
        return preferences;
    }

    [Fact]
    public async Task ProcessSocialEvents_StagesTheDecisionOfEachEventAndCommitsTheBatchOnce()
    {
        var created = SocialEvent.Create(SocialEventType.FriendRequested, Friend, Account, Now);
        var outbox = new Outbox(created);
        var device = NewDevice(Account);
        var useCase = new ProcessSocialEventsUseCase(
            outbox,
            new Friendships(FriendRelationship.Create(Friend, Account, Now)),
            new Preferences(SocialOn(Account)),
            new Devices(device),
            new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new ProcessSocialEventsCommand(), CancellationToken.None);

        Assert.Equal(100, outbox.ReservedBatchSize);
        Assert.Equal(1, result.Processed);
        var (plan, receipt) = Assert.Single(outbox.Staged);
        Assert.Equal(SocialEventOutcome.Created, plan.Outcome);
        Assert.Equal(device.Id, Assert.Single(plan.Deliveries).DeviceId);
        Assert.Equal(created.Id, receipt.EventId);
        Assert.Equal("created", receipt.Outcome);
        Assert.True(outbox.Committed);
        Assert.True(outbox.Disposed);
    }

    [Fact]
    public async Task ProcessSocialEvents_AnEventWithoutRelationshipIsRecordedAsObsoleteWithoutANotification()
    {
        var outbox = new Outbox(SocialEvent.Create(SocialEventType.FriendRequested, Friend, Account, Now));
        var useCase = new ProcessSocialEventsUseCase(
            outbox,
            new Friendships(null),
            new Preferences(SocialOn(Account)),
            new Devices(NewDevice(Account)),
            new FixedTimeProvider(Now));

        await useCase.ExecuteAsync(new ProcessSocialEventsCommand(), CancellationToken.None);

        var (plan, receipt) = Assert.Single(outbox.Staged);
        Assert.Null(plan.Notification);
        Assert.Equal("obsolete", receipt.Outcome);
    }

    [Fact]
    public async Task ProcessSocialEvents_AnEmptyOutboxStillCommitsAndReportsNothingProcessed()
    {
        var outbox = new Outbox();
        var useCase = new ProcessSocialEventsUseCase(
            outbox, new Friendships(null), new Preferences(null), new Devices(), new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new ProcessSocialEventsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.True(outbox.Committed);
    }

    // ---- push delivery -------------------------------------------------------------------------

    private sealed class PushConfig : IPushRegistrationConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public Guid? ProjectId => Guid.NewGuid();

        public int MaxAttempts { get; set; } = 5;
    }

    private sealed class Queue : IPushDeliveryQueue, IPushDeliveryBatch, IReceiptCheckBatch
    {
        public List<PendingPushDelivery> Pending { get; } = [];

        public List<NotificationDelivery> Receipts { get; } = [];

        public List<string> Events { get; } = [];

        public DateTimeOffset? ReservedPendingAt { get; private set; }

        public int ReservedBatchSize { get; private set; }

        IReadOnlyList<PendingPushDelivery> IPushDeliveryBatch.Items => Pending;

        IReadOnlyList<NotificationDelivery> IReceiptCheckBatch.Deliveries => Receipts;

        public Task<IPushDeliveryBatch> ReservePendingAsync(int batchSize, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ReservedBatchSize = batchSize;
            ReservedPendingAt = now;
            Events.Add("reserve-pending");
            return Task.FromResult<IPushDeliveryBatch>(this);
        }

        public Task<IReceiptCheckBatch> ReserveReceiptChecksAsync(int batchSize, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Events.Add("reserve-receipts");
            return Task.FromResult<IReceiptCheckBatch>(this);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Events.Add("commit");
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Events.Add("dispose");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class Sender : IPushSender
    {
        public Func<PushMessage, PushSendResult> OnSend { get; set; } = _ => new PushSendResult(true, "ticket-1", null, null, false, false);

        public Func<string, PushReceiptResult> OnReceipt { get; set; } = _ => new PushReceiptResult(true, true, null, false);

        public List<PushMessage> Sent { get; } = [];

        public List<string> ReceiptsRequested { get; } = [];

        public Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.FromResult(OnSend(message));
        }

        public Task<PushReceiptResult> GetReceiptAsync(string ticketId, CancellationToken cancellationToken)
        {
            ReceiptsRequested.Add(ticketId);
            return Task.FromResult(OnReceipt(ticketId));
        }
    }

    private sealed class Deliveries(params NotificationDelivery[] all) : INotificationDeliveries
    {
        public Task<IReadOnlyList<NotificationDelivery>> ListPendingForCategoriesAsync(
            Guid accountId, IReadOnlyCollection<NotificationCategory> categories, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<NotificationDelivery>> ListOpenForDeviceAsync(Guid deviceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<NotificationDelivery?> FindLeasedAsync(Guid deliveryId, Guid leaseId, CancellationToken cancellationToken) =>
            Task.FromResult(all.SingleOrDefault(delivery => delivery.Id == deliveryId && delivery.LeaseId == leaseId));

        public Task<NotificationDelivery?> FindTicketedAsync(Guid deliveryId, string ticketId, CancellationToken cancellationToken) =>
            Task.FromResult(all.SingleOrDefault(delivery =>
                delivery.Id == deliveryId &&
                delivery.ExpoTicketId == ticketId &&
                delivery.Status == NotificationDeliveryStatus.Ticketed));
    }

    private sealed class Saves : INotificationsUnitOfWork
    {
        public int Count { get; private set; }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Count++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedJitter(double value) : IJitterSource
    {
        public double NextUnit() => value;
    }

    private sealed record Rig(
        DeliverPushNotificationsUseCase UseCase,
        Queue Queue,
        Sender Sender,
        Saves Saves,
        FixedTimeProvider Clock,
        PushConfig Config);

    private static Rig CreateRig(
        IEnumerable<NotificationDelivery> deliveries,
        IEnumerable<NotificationDevice>? devices = null,
        double jitter = 0.5)
    {
        var all = deliveries.ToArray();
        var queue = new Queue();
        var sender = new Sender();
        var saves = new Saves();
        var clock = new FixedTimeProvider(Now);
        var config = new PushConfig();
        var useCase = new DeliverPushNotificationsUseCase(
            config,
            queue,
            sender,
            new Deliveries(all),
            new Devices((devices ?? []).ToArray()),
            saves,
            new FixedJitter(jitter),
            clock);
        return new Rig(useCase, queue, sender, saves, clock, config);
    }

    private static (NotificationDelivery Delivery, NotificationDevice Device, UserNotification Notification, NotificationPreferences Preferences) Pending(
        bool socialEnabled = true,
        bool deviceActive = true)
    {
        var device = NewDevice(Account);
        if (!deviceActive)
        {
            device.Deactivate();
        }

        var notification = UserNotification.Create(Account, NotificationCategory.Social, "k", "Título", "Corpo", "/social", Now);
        var preferences = NotificationPreferences.Create(Account);
        preferences.Update(socialEnabled, false, false);
        return (NotificationDelivery.Create(notification.Id, device.Id, Now), device, notification, preferences);
    }

    private static Rig WithPending(
        (NotificationDelivery Delivery, NotificationDevice Device, UserNotification Notification, NotificationPreferences Preferences) item,
        double jitter = 0.5)
    {
        var rig = CreateRig([item.Delivery], [item.Device], jitter);
        rig.Queue.Pending.Add(new PendingPushDelivery(item.Delivery, item.Notification, item.Device, item.Preferences));
        return rig;
    }

    [Fact]
    public async Task Deliver_WhilePushIsDisabled_DoesNothingAtAll()
    {
        var rig = CreateRig([]);
        rig.Config.IsEnabled = false;

        var result = await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Empty(rig.Queue.Events);
    }

    [Fact]
    public async Task Deliver_ASuccessfulSend_LeasesForTwoMinutesAndStoresTheTicketWithAFifteenMinuteReceiptCheck()
    {
        var item = Pending();
        var rig = WithPending(item);

        var result = await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(50, rig.Queue.ReservedBatchSize);
        Assert.Equal(Now, rig.Queue.ReservedPendingAt);
        var message = Assert.Single(rig.Sender.Sent);
        Assert.Equal(item.Device.ExpoPushToken, message.PushToken);
        Assert.Equal(item.Notification.Id, message.NotificationId);
        Assert.Equal("Título", message.Title);
        Assert.Equal("/social", message.DeepLink);
        Assert.Equal(NotificationDeliveryStatus.Ticketed, item.Delivery.Status);
        Assert.Equal("ticket-1", item.Delivery.ExpoTicketId);
        Assert.Equal(Now.AddMinutes(15), item.Delivery.ReceiptCheckAt);
        Assert.Equal(1, item.Delivery.AttemptCount);
        Assert.Null(item.Delivery.LeaseId);
        Assert.Equal(1, rig.Saves.Count);
        // the reservation is committed before the provider is called, then released
        Assert.Equal(["reserve-pending", "commit", "dispose", "reserve-receipts", "commit", "dispose"], rig.Queue.Events);
    }

    [Fact]
    public async Task Deliver_TheLeaseHeldWhileSending_LastsTwoMinutes()
    {
        var item = Pending();
        var rig = WithPending(item);
        DateTimeOffset? leaseUntil = null;
        rig.Sender.OnSend = _ =>
        {
            leaseUntil = item.Delivery.LeaseUntil;
            return new PushSendResult(true, "t", null, null, false, false);
        };

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(Now.AddMinutes(2), leaseUntil);
    }

    [Fact]
    public async Task Deliver_ACancelledCategoryOrInactiveDevice_IsCancelledAndNeverSent()
    {
        var preferenceOff = Pending(socialEnabled: false);
        var deviceOff = Pending(deviceActive: false);
        var rig = CreateRig([preferenceOff.Delivery, deviceOff.Delivery], [preferenceOff.Device, deviceOff.Device]);
        rig.Queue.Pending.Add(new PendingPushDelivery(preferenceOff.Delivery, preferenceOff.Notification, preferenceOff.Device, preferenceOff.Preferences));
        rig.Queue.Pending.Add(new PendingPushDelivery(deviceOff.Delivery, deviceOff.Notification, deviceOff.Device, deviceOff.Preferences));

        var result = await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Empty(rig.Sender.Sent);
        Assert.Equal("preference_disabled", preferenceOff.Delivery.LastErrorCode);
        Assert.Equal("device_or_notification_inactive", deviceOff.Delivery.LastErrorCode);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, preferenceOff.Delivery.Status);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, deviceOff.Delivery.Status);
    }

    [Theory]
    [InlineData(0.0, 4)]
    [InlineData(0.5, 5)]
    [InlineData(0.99999, 6)]
    public async Task Deliver_ARetryableFailure_BacksOffFiveSecondsWithTheJitterGivenByTheSource(double jitter, double seconds)
    {
        var item = Pending();
        var rig = WithPending(item, jitter);
        rig.Sender.OnSend = _ => new PushSendResult(false, null, "http_500", null, false, true);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Pending, item.Delivery.Status);
        Assert.Equal("http_500", item.Delivery.LastErrorCode);
        Assert.Equal(seconds, (item.Delivery.NextAttemptAt - Now).TotalSeconds, precision: 2);
    }

    [Fact]
    public async Task Deliver_TheProviderRetryAfter_WinsOverTheBackoff()
    {
        var item = Pending();
        var rig = WithPending(item);
        rig.Sender.OnSend = _ => new PushSendResult(false, null, "http_429", TimeSpan.FromSeconds(120), false, true);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(Now.AddSeconds(120), item.Delivery.NextAttemptAt);
    }

    [Fact]
    public async Task Deliver_ARetryableFailureOnTheLastAttempt_FailsTheDelivery()
    {
        var item = Pending();
        var rig = WithPending(item);
        rig.Config.MaxAttempts = 1;
        rig.Sender.OnSend = _ => new PushSendResult(false, null, "http_500", null, false, true);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Failed, item.Delivery.Status);
        Assert.Equal(Now, item.Delivery.CompletedAt);
    }

    [Fact]
    public async Task Deliver_ANonRetryableErrorWithoutACode_FailsAfterOneAttemptWithTheDefaultCode()
    {
        var item = Pending();
        var rig = WithPending(item);
        rig.Sender.OnSend = _ => new PushSendResult(false, null, null, null, false, false);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Failed, item.Delivery.Status);
        Assert.Equal(1, item.Delivery.AttemptCount);
        Assert.Equal("expo_push_rejected", item.Delivery.LastErrorCode);
    }

    [Fact]
    public async Task Deliver_DeviceNotRegistered_FailsAfterOneAttemptAndDeactivatesTheDevice()
    {
        var item = Pending();
        var rig = WithPending(item);
        rig.Sender.OnSend = _ => new PushSendResult(false, null, "DeviceNotRegistered", null, true, false);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Failed, item.Delivery.Status);
        Assert.False(item.Device.IsActive);
    }

    private static (NotificationDelivery Delivery, Rig Rig) Ticketed(int receiptChecks = 0, TimeSpan? ticketAge = null)
    {
        var item = Pending();
        var rig = CreateRig([item.Delivery], [item.Device]);
        var leaseId = Guid.NewGuid();
        var created = Now - (ticketAge ?? TimeSpan.Zero);
        item.Delivery.Claim(leaseId, created.AddMinutes(2), created);
        item.Delivery.RecordTicket(leaseId, "ticket-1", created.AddMinutes(15), created);
        for (var index = 0; index < receiptChecks; index++)
        {
            item.Delivery.RetryReceipt(created.AddMinutes(15), "x");
        }

        rig.Queue.Receipts.Add(item.Delivery);
        return (item.Delivery, rig);
    }

    [Fact]
    public async Task Deliver_ADueReceipt_IsRescheduledAtClaimTimeAndCompletedWhenReady()
    {
        var (delivery, rig) = Ticketed(ticketAge: TimeSpan.FromMinutes(16));

        var result = await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(1, result.Processed);
        Assert.Equal(["ticket-1"], rig.Sender.ReceiptsRequested);
        Assert.Equal(NotificationDeliveryStatus.Completed, delivery.Status);
        Assert.Equal(Now, delivery.CompletedAt);
        Assert.Equal(1, delivery.ReceiptCheckCount);
    }

    [Fact]
    public async Task Deliver_ANotReadyReceipt_IsRescheduledAgainWithTheNextBackoffStep()
    {
        var (delivery, rig) = Ticketed(ticketAge: TimeSpan.FromMinutes(16));
        rig.Sender.OnReceipt = _ => new PushReceiptResult(false, false, null, false);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Ticketed, delivery.Status);
        Assert.Equal(2, delivery.ReceiptCheckCount);
        Assert.Equal(Now.AddMinutes(30), delivery.ReceiptCheckAt);
        Assert.Equal("receipt_not_ready", delivery.LastErrorCode);
    }

    [Fact]
    public async Task Deliver_AReceiptWithDeviceNotRegistered_FailsTheDeliveryAndDeactivatesTheDevice()
    {
        var device = NewDevice(Account);
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), device.Id, Now.AddHours(-1));
        var lease = Guid.NewGuid();
        delivery.Claim(lease, Now.AddHours(-1).AddMinutes(2), Now.AddHours(-1));
        delivery.RecordTicket(lease, "ticket-2", Now.AddMinutes(-45), Now.AddHours(-1));
        var rig = CreateRig([delivery], [device]);
        rig.Queue.Receipts.Add(delivery);
        rig.Sender.OnReceipt = _ => new PushReceiptResult(true, false, "DeviceNotRegistered", true);

        await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(NotificationDeliveryStatus.Failed, delivery.Status);
        Assert.Equal("DeviceNotRegistered", delivery.LastErrorCode);
        Assert.False(device.IsActive);
    }

    [Fact]
    public async Task Deliver_ATicketOlderThanTwentyFourHours_ExpiresWithoutAskingTheProvider()
    {
        var (delivery, rig) = Ticketed(ticketAge: TimeSpan.FromHours(25));

        var result = await rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None);

        Assert.Equal(0, result.Processed);
        Assert.Empty(rig.Sender.ReceiptsRequested);
        Assert.Equal(NotificationDeliveryStatus.Failed, delivery.Status);
        Assert.Equal("receipt_expired", delivery.LastErrorCode);
    }

    [Fact]
    public async Task Deliver_AFailureReadingAReceipt_PropagatesAndStopsTheCycle()
    {
        var (_, rig) = Ticketed(ticketAge: TimeSpan.FromMinutes(16));
        rig.Sender.OnReceipt = _ => throw new IOException("connection reset");

        await Assert.ThrowsAsync<IOException>(() =>
            rig.UseCase.ExecuteAsync(new DeliverPushNotificationsCommand(), CancellationToken.None));
    }
}
