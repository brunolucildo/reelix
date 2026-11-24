using Reelix.Application.Authentication;
using Reelix.Application.Errors;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;
using Reelix.Domain.Users;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Notifications;

/// <summary>The notification use cases with in-memory ports: no database, no HTTP (spec 0014, R6, R7 and R11).</summary>
public sealed class NotificationUseCaseTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;
    private static readonly Guid SubjectId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ProjectId = Guid.Parse("22222222-0000-0000-0000-000000000002");

    private sealed class Accounts : IUserAccountRepository
    {
        public UserAccount? Existing { get; set; }

        public List<string> Calls { get; } = [];

        public Task<UserAccount?> FindAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            Calls.Add("find");
            return Task.FromResult(Existing);
        }

        public Task<UserAccount> EnsureAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            Calls.Add("ensure");
            Existing ??= UserAccount.Create(supabaseUserId, Now);
            return Task.FromResult(Existing);
        }
    }

    private sealed class PushConfiguration : IPushRegistrationConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public Guid? ProjectId { get; set; } = NotificationUseCaseTests.ProjectId;

        public int MaxAttempts { get; set; } = 5;
    }

    /// <summary>An in-memory store behind most of the notification ports.</summary>
    private sealed class Store :
        INotificationPreferencesStore,
        INotificationDevices,
        INotificationDeviceLookup,
        INotificationDeliveries,
        IUserNotifications,
        INotificationsUnitOfWork
    {
        public NotificationPreferences? Preferences { get; set; }

        public List<NotificationDevice> Devices { get; } = [];

        public List<NotificationDelivery> Deliveries { get; } = [];

        public List<UserNotification> Notifications { get; } = [];

        public int Saves { get; private set; }

        public IReadOnlyCollection<NotificationCategory>? RequestedCategories { get; private set; }

        public bool PreferencesAdded { get; private set; }

        public Task<NotificationPreferences?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult(Preferences);

        public void Add(NotificationPreferences preferences)
        {
            PreferencesAdded = true;
            Preferences = preferences;
        }

        public Task<NotificationDevice?> FindByInstallationAsync(Guid installationId, CancellationToken cancellationToken) =>
            Task.FromResult(Devices.SingleOrDefault(device => device.InstallationId == installationId));

        public Task<bool> TokenInUseAsync(string expoPushToken, Guid? exceptDeviceId, CancellationToken cancellationToken) =>
            Task.FromResult(Devices.Any(device =>
                device.ExpoPushToken == expoPushToken && device.IsActive && device.Id != exceptDeviceId));

        public Task<NotificationDevice?> FindActiveOwnedAsync(Guid accountId, Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(Devices.SingleOrDefault(device =>
                device.Id == deviceId && device.AccountId == accountId && device.IsActive));

        public void Add(NotificationDevice device) => Devices.Add(device);

        public Task<IReadOnlyList<NotificationDevice>> ListActiveAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationDevice>>(
                Devices.Where(device => device.AccountId == accountId && device.IsActive).ToArray());

        Task<NotificationDevice?> INotificationDeviceLookup.FindAsync(Guid deviceId, CancellationToken cancellationToken) =>
            Task.FromResult(Devices.SingleOrDefault(device => device.Id == deviceId));

        public Task<IReadOnlyList<NotificationDelivery>> ListPendingForCategoriesAsync(
            Guid accountId,
            IReadOnlyCollection<NotificationCategory> categories,
            CancellationToken cancellationToken)
        {
            RequestedCategories = categories;
            return Task.FromResult<IReadOnlyList<NotificationDelivery>>(
                Deliveries
                    .Where(delivery =>
                        delivery.Status == NotificationDeliveryStatus.Pending &&
                        Notifications.Any(notification =>
                            notification.Id == delivery.NotificationId &&
                            notification.AccountId == accountId &&
                            categories.Contains(notification.Category)))
                    .ToArray());
        }

        public Task<IReadOnlyList<NotificationDelivery>> ListOpenForDeviceAsync(
            Guid deviceId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationDelivery>>(
                Deliveries.Where(delivery =>
                    delivery.DeviceId == deviceId &&
                    delivery.Status is NotificationDeliveryStatus.Pending or NotificationDeliveryStatus.Ticketed)
                    .ToArray());

        public Task<NotificationDelivery?> FindLeasedAsync(Guid deliveryId, Guid leaseId, CancellationToken cancellationToken) =>
            Task.FromResult(Deliveries.SingleOrDefault(delivery => delivery.Id == deliveryId && delivery.LeaseId == leaseId));

        public Task<NotificationDelivery?> FindTicketedAsync(Guid deliveryId, string ticketId, CancellationToken cancellationToken) =>
            Task.FromResult(Deliveries.SingleOrDefault(delivery =>
                delivery.Id == deliveryId &&
                delivery.ExpoTicketId == ticketId &&
                delivery.Status == NotificationDeliveryStatus.Ticketed));

        public Task<IReadOnlyList<NotificationInboxItem>> ListPageAsync(
            Guid accountId,
            NotificationCursor? cursor,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationInboxItem>>(
                Notifications
                    .Where(notification => notification.AccountId == accountId)
                    .OrderByDescending(notification => notification.CreatedAt)
                    .Take(limit)
                    .Select(notification => new NotificationInboxItem(
                        notification.Id,
                        notification.Category.ToApplication(),
                        notification.Title,
                        notification.Body,
                        notification.DeepLink,
                        notification.CreatedAt,
                        notification.ReadAt))
                    .ToArray());

        public Task<int> CountUnreadAsync(Guid accountId, CancellationToken cancellationToken) =>
            Task.FromResult(Notifications.Count(notification => notification.AccountId == accountId && notification.ReadAt is null));

        public Task<UserNotification?> FindOwnedAsync(Guid accountId, Guid notificationId, CancellationToken cancellationToken) =>
            Task.FromResult(Notifications.SingleOrDefault(notification =>
                notification.Id == notificationId && notification.AccountId == accountId));

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Saves++;
            return Task.CompletedTask;
        }
    }

    private static UserNotification Notification(Guid accountId, NotificationCategory category, int minutesAgo = 0) =>
        UserNotification.Create(accountId, category, Guid.NewGuid().ToString("N"), "Título", "Corpo", "/social", Now.AddMinutes(-minutesAgo));

    private static NotificationDevice Device(Guid accountId, Guid? installationId = null, string? token = null) =>
        NotificationDevice.Create(
            accountId,
            installationId ?? Guid.NewGuid(),
            token ?? $"ExponentPushToken[{Guid.NewGuid():N}]",
            NotificationDevicePlatform.Android,
            ProjectId,
            Now);

    // ---- preferences -------------------------------------------------------------------------

    [Fact]
    public async Task GetPreferences_OfAnAccountThatDoesNotExistYet_AreAllOffAndNothingIsCreated()
    {
        var accounts = new Accounts();
        var store = new Store();
        var useCase = new GetNotificationPreferencesUseCase(accounts, store);

        var result = await useCase.ExecuteAsync(new GetNotificationPreferencesQuery(SubjectId), CancellationToken.None);

        Assert.Equal(new GetNotificationPreferencesResult(false, false, false), result);
        Assert.Equal(["find"], accounts.Calls);
        Assert.False(store.PreferencesAdded);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task GetPreferences_OfAnAccountWithoutAPreferencesRow_AreAllOffAndNothingIsCreated()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var store = new Store();
        var useCase = new GetNotificationPreferencesUseCase(accounts, store);

        var result = await useCase.ExecuteAsync(new GetNotificationPreferencesQuery(SubjectId), CancellationToken.None);

        Assert.Equal(new GetNotificationPreferencesResult(false, false, false), result);
        Assert.False(store.PreferencesAdded);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task GetPreferences_ReturnsTheSavedValues()
    {
        var account = UserAccount.Create(SubjectId, Now);
        var saved = NotificationPreferences.Create(account.Id);
        saved.Update(true, false, true);
        var useCase = new GetNotificationPreferencesUseCase(
            new Accounts { Existing = account },
            new Store { Preferences = saved });

        var result = await useCase.ExecuteAsync(new GetNotificationPreferencesQuery(SubjectId), CancellationToken.None);

        Assert.Equal(new GetNotificationPreferencesResult(true, false, true), result);
    }

    [Fact]
    public async Task UpdatePreferences_CreatesTheRowAndCancelsPendingDeliveriesOfTheCategoriesTurnedOff()
    {
        var accounts = new Accounts();
        var store = new Store();
        var account = await accounts.EnsureAsync(SubjectId, CancellationToken.None);
        accounts.Calls.Clear();
        var device = Device(account.Id);
        var social = Notification(account.Id, NotificationCategory.Social);
        var newEpisode = Notification(account.Id, NotificationCategory.NewEpisode);
        store.Notifications.AddRange([social, newEpisode]);
        var socialDelivery = NotificationDelivery.Create(social.Id, device.Id, Now);
        var episodeDelivery = NotificationDelivery.Create(newEpisode.Id, device.Id, Now);
        store.Deliveries.AddRange([socialDelivery, episodeDelivery]);
        var useCase = new UpdateNotificationPreferencesUseCase(accounts, store, store, store, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            new UpdateNotificationPreferencesCommand(SubjectId, false, true, false),
            CancellationToken.None);

        Assert.Equal(["ensure"], accounts.Calls);
        Assert.True(store.PreferencesAdded);
        Assert.Equal(new UpdateNotificationPreferencesResult(false, true, false), result);
        Assert.Equal(
            [NotificationCategory.Social, NotificationCategory.Premiere],
            store.RequestedCategories!.Order());
        Assert.Equal(NotificationDeliveryStatus.Cancelled, socialDelivery.Status);
        Assert.Equal("preference_disabled", socialDelivery.LastErrorCode);
        Assert.Equal(Now, socialDelivery.CompletedAt);
        Assert.Equal(NotificationDeliveryStatus.Pending, episodeDelivery.Status);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task UpdatePreferences_WithEveryCategoryOn_CancelsNothing()
    {
        var accounts = new Accounts();
        var store = new Store();
        var useCase = new UpdateNotificationPreferencesUseCase(accounts, store, store, store, new FixedTimeProvider(Now));

        await useCase.ExecuteAsync(
            new UpdateNotificationPreferencesCommand(SubjectId, true, true, true),
            CancellationToken.None);

        Assert.Empty(store.RequestedCategories!);
        Assert.Equal(1, store.Saves);
    }

    // ---- devices -----------------------------------------------------------------------------

    private static RegisterNotificationDeviceUseCase RegisterUseCase(
        Accounts accounts,
        Store store,
        PushConfiguration? configuration = null) =>
        new(accounts, configuration ?? new PushConfiguration(), store, store, store, new FixedTimeProvider(Now));

    private static RegisterNotificationDeviceCommand Registration(
        Guid? installationId = null,
        string? token = "ExponentPushToken[aaaaaaaaaaaaaaaaaaaaaa]",
        Guid? projectId = null,
        DevicePlatform platform = DevicePlatform.Android) =>
        new(SubjectId, installationId ?? Guid.NewGuid(), token, platform, projectId ?? ProjectId);

    [Fact]
    public async Task RegisterDevice_WithoutAToken_IsAValidationErrorBeforeAnythingElse()
    {
        var accounts = new Accounts();
        var useCase = RegisterUseCase(accounts, new Store(), new PushConfiguration { IsEnabled = false });

        var error = await Assert.ThrowsAsync<ValidationError>(() =>
            useCase.ExecuteAsync(Registration(token: null), CancellationToken.None));

        Assert.Equal("Informe um token Expo.", error.Message);
        Assert.Empty(accounts.Calls);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task RegisterDevice_WhilePushIsOffOrForAnotherProject_IsServiceUnavailableBeforeTouchingTheAccount(
        bool enabled,
        bool wrongProject)
    {
        var accounts = new Accounts();
        var useCase = RegisterUseCase(accounts, new Store(), new PushConfiguration { IsEnabled = enabled });

        var error = await Assert.ThrowsAsync<ServiceUnavailableError>(() => useCase.ExecuteAsync(
            Registration(projectId: wrongProject ? Guid.NewGuid() : null),
            CancellationToken.None));

        Assert.Equal("Notificações push indisponíveis.", error.Title);
        Assert.Equal(
            "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
            error.Message);
        Assert.Empty(accounts.Calls);
    }

    [Fact]
    public async Task RegisterDevice_WithoutAConfiguredProject_IsServiceUnavailable()
    {
        var useCase = RegisterUseCase(
            new Accounts(),
            new Store(),
            new PushConfiguration { IsEnabled = true, ProjectId = null });

        await Assert.ThrowsAsync<ServiceUnavailableError>(() =>
            useCase.ExecuteAsync(Registration(), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterDevice_ANewInstallation_IsCreatedActiveAndSavedOnce()
    {
        var accounts = new Accounts();
        var store = new Store();
        var useCase = RegisterUseCase(accounts, store);

        var result = await useCase.ExecuteAsync(
            Registration(platform: DevicePlatform.Ios),
            CancellationToken.None);

        var device = Assert.Single(store.Devices);
        Assert.Equal(accounts.Existing!.Id, device.AccountId);
        Assert.True(device.IsActive);
        Assert.Equal(device.Id, result.Id);
        Assert.Equal(DevicePlatform.Ios, result.Platform);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Equal(Now, result.LastRegisteredAt);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task RegisterDevice_ATokenOfAnotherActiveDevice_IsAConflict()
    {
        var store = new Store();
        var other = Device(Guid.NewGuid(), token: "ExponentPushToken[aaaaaaaaaaaaaaaaaaaaaa]");
        store.Devices.Add(other);
        var useCase = RegisterUseCase(new Accounts(), store);

        var error = await Assert.ThrowsAsync<NotificationDeviceConflictException>(() =>
            useCase.ExecuteAsync(Registration(), CancellationToken.None));

        Assert.Equal("Dispositivo já associado.", error.Title);
        Assert.Equal(
            "Este token já está associado a outro dispositivo. Remova o vínculo antigo antes de registrar novamente.",
            error.Message);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task RegisterDevice_AnInstallationStillActiveForAnotherAccount_IsAConflict()
    {
        var store = new Store();
        var installation = Guid.NewGuid();
        store.Devices.Add(Device(Guid.NewGuid(), installation));
        var useCase = RegisterUseCase(new Accounts(), store);

        var error = await Assert.ThrowsAsync<NotificationDeviceConflictException>(() =>
            useCase.ExecuteAsync(Registration(installation), CancellationToken.None));

        Assert.Equal(
            "Este dispositivo ainda está associado a outra conta. Encerre a sessão anterior com conexão e tente novamente.",
            error.Message);
        Assert.IsAssignableFrom<ConflictError>(error);
    }

    [Fact]
    public async Task RegisterDevice_AgainForTheSameInstallation_UpdatesItAndCancelsItsOpenDeliveries()
    {
        var accounts = new Accounts();
        var account = await accounts.EnsureAsync(SubjectId, CancellationToken.None);
        var store = new Store();
        var installation = Guid.NewGuid();
        var device = Device(account.Id, installation);
        store.Devices.Add(device);
        var notification = Notification(account.Id, NotificationCategory.Social);
        store.Notifications.Add(notification);
        var pending = NotificationDelivery.Create(notification.Id, device.Id, Now);
        store.Deliveries.Add(pending);
        var clock = new FixedTimeProvider(Now.AddMinutes(10));
        var useCase = new RegisterNotificationDeviceUseCase(
            accounts, new PushConfiguration(), store, store, store, clock);

        var result = await useCase.ExecuteAsync(
            Registration(installation, "ExponentPushToken[bbbbbbbbbbbbbbbbbbbbbb]"),
            CancellationToken.None);

        Assert.Equal(device.Id, result.Id);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Equal(Now.AddMinutes(10), result.LastRegisteredAt);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, pending.Status);
        Assert.Equal("account_changed", pending.LastErrorCode);
        Assert.Single(store.Devices);
    }

    [Fact]
    public async Task RegisterDevice_AnUndefinedPlatformIsRejectedByTheDomainWithItsOwnMessage()
    {
        var useCase = RegisterUseCase(new Accounts(), new Store());

        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            useCase.ExecuteAsync(Registration(platform: (DevicePlatform)7), CancellationToken.None));

        Assert.Equal("platform", error.ParamName);
    }

    [Fact]
    public async Task DeactivateDevice_WithoutAnAccount_IsNotFoundAndCreatesNothing()
    {
        var accounts = new Accounts();
        var store = new Store();
        var useCase = new DeactivateNotificationDeviceUseCase(accounts, store, store, store, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            new DeactivateNotificationDeviceCommand(SubjectId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.Deactivated);
        Assert.Equal(["find"], accounts.Calls);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task DeactivateDevice_OfAnotherAccount_IsNotFound()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var store = new Store();
        var foreign = Device(Guid.NewGuid());
        store.Devices.Add(foreign);
        var useCase = new DeactivateNotificationDeviceUseCase(accounts, store, store, store, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            new DeactivateNotificationDeviceCommand(SubjectId, foreign.Id),
            CancellationToken.None);

        Assert.False(result.Deactivated);
        Assert.True(foreign.IsActive);
    }

    [Fact]
    public async Task DeactivateDevice_DeactivatesItAndCancelsItsOpenDeliveries()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var store = new Store();
        var device = Device(accounts.Existing.Id);
        store.Devices.Add(device);
        var notification = Notification(accounts.Existing.Id, NotificationCategory.Social);
        store.Notifications.Add(notification);
        var pending = NotificationDelivery.Create(notification.Id, device.Id, Now);
        store.Deliveries.Add(pending);
        var useCase = new DeactivateNotificationDeviceUseCase(accounts, store, store, store, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(
            new DeactivateNotificationDeviceCommand(SubjectId, device.Id),
            CancellationToken.None);

        Assert.True(result.Deactivated);
        Assert.False(device.IsActive);
        Assert.Equal(NotificationDeliveryStatus.Cancelled, pending.Status);
        Assert.Equal("device_removed", pending.LastErrorCode);
        Assert.Equal(1, store.Saves);
    }

    // ---- inbox -------------------------------------------------------------------------------

    [Fact]
    public async Task Inbox_OfAnAccountThatDoesNotExistYet_IsEmptyAndNothingIsCreated()
    {
        var accounts = new Accounts();
        var useCase = new GetNotificationInboxUseCase(accounts, new Store());

        var result = await useCase.ExecuteAsync(new GetNotificationInboxQuery(SubjectId, null), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
        Assert.Equal(["find"], accounts.Calls);
    }

    [Fact]
    public async Task Inbox_ReturnsTwentyFiveItemsAndACursorOfTheLastOneWhenThereIsMore()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var store = new Store();
        for (var index = 0; index < 26; index++)
        {
            store.Notifications.Add(Notification(accounts.Existing.Id, NotificationCategory.Social, index));
        }

        var useCase = new GetNotificationInboxUseCase(accounts, store);

        var result = await useCase.ExecuteAsync(new GetNotificationInboxQuery(SubjectId, null), CancellationToken.None);

        Assert.Equal(25, result.Items.Count);
        var last = result.Items[^1];
        Assert.Equal(
            Reelix.Application.Common.CursorCodec.EncodeJson(new NotificationCursor(last.CreatedAt, last.Id)),
            result.NextCursor);
    }

    [Fact]
    public async Task Inbox_WithExactlyOnePage_HasNoCursor()
    {
        var accounts = new Accounts { Existing = UserAccount.Create(SubjectId, Now) };
        var store = new Store();
        for (var index = 0; index < 25; index++)
        {
            store.Notifications.Add(Notification(accounts.Existing.Id, NotificationCategory.Social, index));
        }

        var result = await new GetNotificationInboxUseCase(accounts, store)
            .ExecuteAsync(new GetNotificationInboxQuery(SubjectId, null), CancellationToken.None);

        Assert.Equal(25, result.Items.Count);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task Inbox_AnInvalidCursorIsRejectedBeforeLookingAtTheAccount()
    {
        var accounts = new Accounts();
        var useCase = new GetNotificationInboxUseCase(accounts, new Store());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            useCase.ExecuteAsync(new GetNotificationInboxQuery(SubjectId, "%%%"), CancellationToken.None));

        Assert.Empty(accounts.Calls);
    }

    [Fact]
    public async Task UnreadCount_CountsOnlyTheUnreadOnesOfTheAccountAndIsZeroWithoutAnAccount()
    {
        var store = new Store();
        var account = UserAccount.Create(SubjectId, Now);
        var read = Notification(account.Id, NotificationCategory.Social);
        read.MarkRead(Now);
        store.Notifications.AddRange(
        [
            read,
            Notification(account.Id, NotificationCategory.Social),
            Notification(Guid.NewGuid(), NotificationCategory.Social),
        ]);

        var known = await new GetNotificationUnreadCountUseCase(new Accounts { Existing = account }, store)
            .ExecuteAsync(new GetNotificationUnreadCountQuery(SubjectId), CancellationToken.None);
        var accounts = new Accounts();
        var unknown = await new GetNotificationUnreadCountUseCase(accounts, store)
            .ExecuteAsync(new GetNotificationUnreadCountQuery(SubjectId), CancellationToken.None);

        Assert.Equal(1, known.Count);
        Assert.Equal(0, unknown.Count);
        Assert.Equal(["find"], accounts.Calls);
    }

    [Fact]
    public async Task MarkRead_MarksTheOwnNotificationAndRefusesTheOthers()
    {
        var store = new Store();
        var account = UserAccount.Create(SubjectId, Now);
        var own = Notification(account.Id, NotificationCategory.Social);
        var foreign = Notification(Guid.NewGuid(), NotificationCategory.Social);
        store.Notifications.AddRange([own, foreign]);
        var useCase = new MarkNotificationReadUseCase(
            new Accounts { Existing = account }, store, store, new FixedTimeProvider(Now));

        var marked = await useCase.ExecuteAsync(new MarkNotificationReadCommand(SubjectId, own.Id), CancellationToken.None);
        var refused = await useCase.ExecuteAsync(new MarkNotificationReadCommand(SubjectId, foreign.Id), CancellationToken.None);
        var unknown = await useCase.ExecuteAsync(new MarkNotificationReadCommand(SubjectId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(marked.Marked);
        Assert.Equal(Now, own.ReadAt);
        Assert.False(refused.Marked);
        Assert.Null(foreign.ReadAt);
        Assert.False(unknown.Marked);
        Assert.Equal(1, store.Saves);
    }

    [Fact]
    public async Task MarkRead_WithoutAnAccount_IsNotFoundAndCreatesNothing()
    {
        var accounts = new Accounts();
        var store = new Store();
        var useCase = new MarkNotificationReadUseCase(accounts, store, store, new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(new MarkNotificationReadCommand(SubjectId, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Marked);
        Assert.Equal(["find"], accounts.Calls);
    }

    // ---- the shared rules ----------------------------------------------------------------------

    [Fact]
    public void TheMirrorEnumsHaveTheSameNamesAndValuesAsTheDomainOnes()
    {
        Assert.Equal(Members<NotificationDevicePlatform>(), Members<DevicePlatform>());
        Assert.Equal(Members<NotificationCategory>(), Members<NotificationCategoryKind>());
        Assert.Equal(
            Members<Reelix.Domain.Gamification.UserAchievementStatus>(),
            Members<Reelix.Application.Gamification.AchievementStatus>());
    }

    private static (string Name, int Value)[] Members<T>()
        where T : struct, Enum =>
        Enum.GetValues<T>().Select(value => (value.ToString(), Convert.ToInt32(value))).ToArray();

    [Fact]
    public void TheConversionsPreserveTheValueEvenWhenItIsUndefined()
    {
        Assert.Equal(NotificationDevicePlatform.Ios, DevicePlatform.Ios.ToDomain());
        Assert.Equal(NotificationDevicePlatform.Android, DevicePlatform.Android.ToDomain());
        Assert.Equal((NotificationDevicePlatform)7, ((DevicePlatform)7).ToDomain());
        Assert.Equal(DevicePlatform.Android, NotificationDevicePlatform.Android.ToApplication());
        Assert.Equal(NotificationCategoryKind.Premiere, NotificationCategory.Premiere.ToApplication());
    }
}
