using System.Reflection;
using Reelix.Application.Authentication;
using Reelix.Application.Common;
using Reelix.Application.Gamification;
using Reelix.Application.Notifications;
using Reelix.Application.Social;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Common;

/// <summary>
/// Pins the opaque cursors (spec 0013, R7). The golden strings were produced by the code before
/// the codec existed (the private <c>EncodeCursor</c> of the social and notification services and
/// the ranking cursor built by the gamification repository), so a flow can only keep emitting and
/// accepting the cursors that clients already hold.
/// </summary>
public sealed class CursorCodecTests
{
    private const string SocialGolden =
        "eyJPY2N1cnJlZEF0IjoiMjAyNi0wMy0wNFQwNTowNjowNy4wODkrMDA6MDAiLCJJZCI6IjExMTExMTExLTIyMjItMzMzMy00NDQ0LTU1NTU1NTU1NTU1NSJ9";

    private const string NotificationGolden =
        "eyJDcmVhdGVkQXQiOiIyMDI2LTAzLTA0VDA1OjA2OjA3LjA4OSswMDowMCIsIklkIjoiMTExMTExMTEtMjIyMi0zMzMzLTQ0NDQtNTU1NTU1NTU1NTU1In0";

    private const string RankingGolden = "NToxMTExMTExMS0yMjIyLTMzMzMtNDQ0NC01NTU1NTU1NTU1NTU";

    private static readonly DateTimeOffset Moment = DateTimeOffset.Parse("2026-03-04T05:06:07.0890000+00:00");

    private static readonly Guid CursorId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void EncodeJson_ReproducesTheSocialCursorOfTheOldCode() =>
        Assert.Equal(SocialGolden, CursorCodec.EncodeJson(new SocialCursor(Moment, CursorId)));

    [Fact]
    public void EncodeJson_ReproducesTheNotificationCursorOfTheOldCode() =>
        Assert.Equal(
            NotificationGolden,
            CursorCodec.EncodeJson(new NotificationCursor(Moment, CursorId)));

    [Fact]
    public void EncodeText_ReproducesTheRankingCursorOfTheOldCode() =>
        Assert.Equal(RankingGolden, CursorCodec.EncodeText($"5:{CursorId}"));

    [Fact]
    public void DecodeJson_ReadsTheOldCursorsAndRoundTripsThem()
    {
        var social = CursorCodec.DecodeJson<SocialCursor>(SocialGolden, Invalid);
        var notification = CursorCodec.DecodeJson<NotificationCursor>(NotificationGolden, Invalid);

        Assert.Equal(new SocialCursor(Moment, CursorId), social);
        Assert.Equal(new NotificationCursor(Moment, CursorId), notification);
        Assert.Equal(SocialGolden, CursorCodec.EncodeJson(social!));
        Assert.Equal(NotificationGolden, CursorCodec.EncodeJson(notification!));
    }

    [Fact]
    public void TryDecodeText_ReadsTheOldRankingCursor()
    {
        Assert.True(CursorCodec.TryDecodeText(RankingGolden, out var text));
        Assert.Equal($"5:{CursorId}", text);
    }

    [Fact]
    public void Encoded_UsesTheUrlAlphabetWithoutPadding()
    {
        // 0xFB 0xFF encodes to "+/8=" in standard base64.
        var encoded = CursorCodec.EncodeText(System.Text.Encoding.Latin1.GetString([0xFB, 0xFF]));

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
    }

    [Fact]
    public void DecodeJson_AcceptsPaddedAndUnpaddedInput()
    {
        var padded = SocialGolden + new string('=', (4 - SocialGolden.Length % 4) % 4);

        Assert.Equal(
            CursorCodec.DecodeJson<SocialCursor>(SocialGolden, Invalid),
            CursorCodec.DecodeJson<SocialCursor>(padded, Invalid));
    }

    [Fact]
    public void DecodeJson_RejectsACursorAboveTheLimitWithoutAnInnerException()
    {
        var tooLong = new string('A', CursorCodec.MaxLength + 1);

        var exception = Assert.Throws<ArgumentException>(() =>
            CursorCodec.DecodeJson<SocialCursor>(tooLong, Invalid));

        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("%%%not-base64%%%")]
    [InlineData("bm90LWpzb24")]
    public void DecodeJson_ReportsTheDecodingFailureAsTheInnerException(string cursor)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            CursorCodec.DecodeJson<SocialCursor>(cursor, Invalid));

        Assert.NotNull(exception.InnerException);
    }

    [Fact]
    public void TryDecodeText_RefusesInvalidInput()
    {
        Assert.False(CursorCodec.TryDecodeText("%%%", out _));
        Assert.False(CursorCodec.TryDecodeText(new string('A', CursorCodec.MaxLength + 1), out _));
    }

    [Fact]
    public async Task SocialFeed_AcceptsTheOldCursorAndEmitsTheSameNextCursor()
    {
        SocialCursor? received = null;
        var items = Enumerable.Range(0, 21)
            .Select(index => new FriendItem(
                index == 19 ? CursorId : Guid.NewGuid(),
                Guid.NewGuid(),
                "Amigo",
                index == 19 ? Moment : Moment.AddDays(-index - 1)))
            .ToArray();
        var repository = StubRepository<ISocialRepository>.Create((method, args) =>
        {
            Assert.Equal(nameof(ISocialRepository.GetFriendsPageAsync), method.Name);
            received = (SocialCursor?)args![1];
            return Task.FromResult<IReadOnlyList<FriendItem>>(items);
        });
        var service = new SocialService(new KnownAccounts(), repository, FixedTimeProvider.AtDefault());

        var page = await service.GetFriendsAsync(Guid.NewGuid(), SocialGolden, CancellationToken.None);

        Assert.Equal(new SocialCursor(Moment, CursorId), received);
        Assert.Equal(SocialGolden, page.NextCursor);
    }

    [Theory]
    [InlineData("%%%")]
    [InlineData("bm90LWpzb24")]
    [InlineData("bnVsbA")]
    public async Task SocialFeed_RejectsAnInvalidCursorWithTheFeedMessage(string cursor)
    {
        var service = new SocialService(
            new KnownAccounts(),
            StubRepository<ISocialRepository>.Create((method, _) => throw new NotSupportedException(method.Name)),
            FixedTimeProvider.AtDefault());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetFriendsAsync(Guid.NewGuid(), cursor, CancellationToken.None));

        Assert.Equal("encodedCursor", exception.ParamName);
        Assert.Equal(
            "O cursor do feed é inválido. (Parameter 'encodedCursor')",
            exception.Message);
    }

    [Fact]
    public async Task NotificationInbox_AcceptsTheOldCursorAndEmitsTheSameNextCursor()
    {
        NotificationCursor? received = null;
        var items = Enumerable.Range(0, 26)
            .Select(index => new NotificationInboxItem(
                index == 24 ? CursorId : Guid.NewGuid(),
                Reelix.Domain.Notifications.NotificationCategory.Social,
                "Título",
                "Mensagem",
                "/social",
                index == 24 ? Moment : Moment.AddDays(-index - 1),
                null))
            .ToArray();
        var repository = StubRepository<INotificationsRepository>.Create((method, args) =>
        {
            Assert.Equal(nameof(INotificationsRepository.GetInboxPageAsync), method.Name);
            received = (NotificationCursor?)args![1];
            return Task.FromResult<IReadOnlyList<NotificationInboxItem>>(items);
        });
        var service = new NotificationsService(new KnownAccounts(), repository, null!);

        var page = await service.GetInboxAsync(Guid.NewGuid(), NotificationGolden, CancellationToken.None);

        Assert.Equal(new NotificationCursor(Moment, CursorId), received);
        Assert.Equal(NotificationGolden, page.NextCursor);
    }

    [Theory]
    [InlineData("%%%")]
    [InlineData("bm90LWpzb24")]
    public async Task NotificationInbox_RejectsAnInvalidCursorWithTheInboxMessage(string cursor)
    {
        var service = new NotificationsService(new KnownAccounts(), null!, null!);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetInboxAsync(Guid.NewGuid(), cursor, CancellationToken.None));

        Assert.Equal("encodedCursor", exception.ParamName);
        Assert.Equal(
            "O cursor da caixa de entrada é inválido. (Parameter 'encodedCursor')",
            exception.Message);
    }

    [Fact]
    public async Task Ranking_AcceptsTheOldCursor()
    {
        GamificationRankingCursor? received = null;
        var repository = StubRepository<IGamificationRepository>.Create((method, args) =>
        {
            Assert.Equal(nameof(IGamificationRepository.GetMonthlyRankingAsync), method.Name);
            received = (GamificationRankingCursor?)args![3];
            return Task.FromResult(new GamificationRankingPage(
                true, DateTimeOffset.MinValue, DateTimeOffset.MinValue, 0, [], null));
        });
        var service = new GamificationService(repository, new KnownAccounts());

        await service.GetMonthlyRankingAsync(
            Guid.NewGuid(), 2026, 11, RankingGolden, 20, CancellationToken.None);

        Assert.Equal(new GamificationRankingCursor(5, CursorId), received);
    }

    [Theory]
    [InlineData("%%%")]
    [InlineData("bm90LWNvdW50")]
    [InlineData("NDI6bm90LWd1aWQ")]
    public async Task Ranking_RejectsAnInvalidCursorWithTheRankingMessage(string cursor)
    {
        var service = new GamificationService(null!, new KnownAccounts());

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetMonthlyRankingAsync(Guid.NewGuid(), 2026, 11, cursor, 20, CancellationToken.None));

        Assert.Equal("cursor", exception.ParamName);
        Assert.Equal(
            "O cursor de paginação é inválido. (Parameter 'cursor')",
            exception.Message);
    }

    private static ArgumentException Invalid(Exception? inner) => new("invalid", "cursor", inner);

    private sealed class KnownAccounts : IUserAccountRepository
    {
        public Task<UserAccount?> FindAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
            Task.FromResult<UserAccount?>(UserAccount.Create(supabaseUserId, FixedTimeProvider.DefaultNow));

        public Task<UserAccount> EnsureAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
            Task.FromResult(UserAccount.Create(supabaseUserId, FixedTimeProvider.DefaultNow));
    }
}

/// <summary>A repository fake for wide ports: the test answers only the calls it expects.</summary>
internal class StubRepository<T> : DispatchProxy
    where T : class
{
    private Func<MethodInfo, object?[]?, object?> _handler = (method, _) =>
        throw new NotSupportedException(method.Name);

    public static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = Create<T, StubRepository<T>>();
        ((StubRepository<T>)(object)proxy)._handler = handler;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
        _handler(targetMethod!, args);
}
