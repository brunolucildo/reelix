using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Lists;
using Reelix.Application.Users;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Lists;

public sealed class UserListsServiceTests
{
    [Fact]
    public async Task AddFavorite_UsesExistingFavoriteRelationAndAuthenticatedAccount()
    {
        var supabaseId = Guid.NewGuid();
        var accounts = new FakeAccounts(supabaseId);
        var repository = new FakeListsRepository { CatalogTitleExists = true };
        var preferences = new FakePreferences();
        var service = new UserListsService(accounts, preferences, repository);

        var inserted = await service.AddFavoriteAsync(
            supabaseId,
            Guid.NewGuid(),
            CatalogContentType.Movie,
            CancellationToken.None);

        Assert.True(inserted);
        Assert.Equal(accounts.Account.Id, repository.FavoriteAccountId);
        Assert.Equal("movie", repository.FavoriteContentType);
        Assert.Equal(accounts.Account.Id, preferences.AccountId);
    }

    [Fact]
    public async Task AddFavorite_ReturnsFalseForAnIdempotentDuplicate()
    {
        var supabaseId = Guid.NewGuid();
        var repository = new FakeListsRepository
        {
            CatalogTitleExists = true,
            FavoriteAlreadyExists = true,
        };
        var service = new UserListsService(
            new FakeAccounts(supabaseId),
            new FakePreferences(),
            repository);

        var inserted = await service.AddFavoriteAsync(
            supabaseId,
            Guid.NewGuid(),
            CatalogContentType.Series,
            CancellationToken.None);

        Assert.False(inserted);
        Assert.Equal("series", repository.FavoriteContentType);
    }

    [Fact]
    public async Task AddWatchlistTitle_RejectsTitleNotInCatalog()
    {
        var supabaseId = Guid.NewGuid();
        var repository = new FakeListsRepository { CatalogTitleExists = false };
        var service = new UserListsService(
            new FakeAccounts(supabaseId),
            new FakePreferences(),
            repository);

        await Assert.ThrowsAsync<CatalogTitleNotFoundException>(() =>
            service.AddWatchlistTitleAsync(
                supabaseId,
                Guid.NewGuid(),
                CatalogContentType.Movie,
                CancellationToken.None));

        Assert.Null(repository.WatchlistAccountId);
    }

    [Fact]
    public async Task GetCustomList_UsesAccountResolvedFromAuthenticatedSubject()
    {
        var supabaseId = Guid.NewGuid();
        var accounts = new FakeAccounts(supabaseId);
        var listId = Guid.NewGuid();
        var repository = new FakeListsRepository { ListOwnerId = accounts.Account.Id };
        var service = new UserListsService(accounts, new FakePreferences(), repository);

        var result = await service.GetCustomListAsync(
            supabaseId,
            listId,
            1,
            20,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(accounts.Account.Id, repository.ReadListAccountId);
        Assert.NotEqual(supabaseId, repository.ReadListAccountId);
    }

    [Fact]
    public async Task GetCustomList_DoesNotReturnAnotherAccountsPrivateList()
    {
        var supabaseId = Guid.NewGuid();
        var accounts = new FakeAccounts(supabaseId);
        var repository = new FakeListsRepository { ListOwnerId = Guid.NewGuid() };
        var service = new UserListsService(accounts, new FakePreferences(), repository);

        var result = await service.GetCustomListAsync(
            supabaseId,
            Guid.NewGuid(),
            1,
            20,
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(accounts.Account.Id, repository.ReadListAccountId);
    }

    [Fact]
    public async Task GetFavorites_RejectsInvalidPagination()
    {
        var service = new UserListsService(
            new FakeAccounts(Guid.NewGuid()),
            new FakePreferences(),
            new FakeListsRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetFavoritesAsync(Guid.NewGuid(), 0, 101, CancellationToken.None));
    }

    [Fact]
    public async Task GetTitleStatuses_RejectsDuplicateIds()
    {
        var titleId = Guid.NewGuid();
        var service = new UserListsService(
            new FakeAccounts(Guid.NewGuid()),
            new FakePreferences(),
            new FakeListsRepository());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetTitleStatusesAsync(
                Guid.NewGuid(),
                [titleId, titleId],
                CancellationToken.None));
    }

    [Fact]
    public async Task GetTitleStatuses_ScopesRelationsToAuthenticatedAccount()
    {
        var supabaseId = Guid.NewGuid();
        var accounts = new FakeAccounts(supabaseId);
        var repository = new FakeListsRepository();
        var service = new UserListsService(accounts, new FakePreferences(), repository);

        await service.GetTitleStatusesAsync(
            supabaseId,
            [Guid.NewGuid()],
            CancellationToken.None);

        Assert.Equal(accounts.Account.Id, repository.StatusAccountId);
    }

    private sealed class FakeAccounts(Guid supabaseUserId) : IUserAccountRepository
    {
        public UserAccount Account { get; } = UserAccount.Create(supabaseUserId);

        public Task<UserAccount> GetOrCreateAsync(Guid subject, CancellationToken cancellationToken) =>
            Task.FromResult(subject == supabaseUserId
                ? Account
                : throw new InvalidOperationException("Unexpected authenticated subject."));
    }

    private sealed class FakePreferences : IUserPreferencesRepository
    {
        public Guid? AccountId { get; private set; }

        public Task<OnboardingPreferences?> GetExistingAsync(
            Guid accountId,
            CancellationToken cancellationToken) =>
            Task.FromResult<OnboardingPreferences?>(null);

        public Task<OnboardingPreferences> GetOrCreateAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            AccountId = accountId;
            return Task.FromResult(new OnboardingPreferences(false, OnboardingStep.Welcome, [], [], [], []));
        }

        public Task SaveProgressAsync(
            Guid accountId,
            SaveOnboardingProgress progress,
            CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CompleteAsync(Guid accountId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> CatalogTitlesExistAsync(
            IReadOnlyCollection<Guid> titleIds,
            CatalogContentType contentType,
            CancellationToken cancellationToken) => Task.FromResult(true);
    }

    private sealed class FakeListsRepository : IUserListsRepository
    {
        public bool CatalogTitleExists { get; init; }

        public bool FavoriteAlreadyExists { get; init; }

        public Guid? FavoriteAccountId { get; private set; }

        public string? FavoriteContentType { get; private set; }

        public Guid? WatchlistAccountId { get; private set; }

        public string? WatchlistContentType { get; private set; }

        public Guid? ReadListAccountId { get; private set; }

        public Guid? StatusAccountId { get; private set; }

        public Guid? ListOwnerId { get; init; }

        public Task<IReadOnlyList<TitleRelationStatus>> GetTitleStatusesAsync(
            Guid accountId,
            IReadOnlyCollection<Guid> titleIds,
            CancellationToken cancellationToken)
        {
            StatusAccountId = accountId;
            return Task.FromResult<IReadOnlyList<TitleRelationStatus>>(
                titleIds.Select(titleId => new TitleRelationStatus(titleId, false, false)).ToArray());
        }

        public Task<bool> CatalogTitleExistsAsync(
            Guid titleId,
            string contentType,
            CancellationToken cancellationToken) =>
            Task.FromResult(CatalogTitleExists);

        public Task<PagedTitleList> GetFavoritesAsync(
            Guid accountId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedTitleList([], page, pageSize, 0));

        public Task<bool> AddFavoriteAsync(
            Guid accountId,
            Guid titleId,
            string contentType,
            CancellationToken cancellationToken)
        {
            FavoriteAccountId = accountId;
            FavoriteContentType = contentType;
            return Task.FromResult(!FavoriteAlreadyExists);
        }

        public Task RemoveFavoriteAsync(Guid accountId, Guid titleId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<PagedTitleList> GetWatchlistAsync(
            Guid accountId,
            int page,
            int pageSize,
            bool oldestFirst,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedTitleList([], page, pageSize, 0));

        public Task<bool> AddWatchlistTitleAsync(
            Guid accountId,
            Guid titleId,
            string contentType,
            CancellationToken cancellationToken)
        {
            WatchlistAccountId = accountId;
            WatchlistContentType = contentType;
            return Task.FromResult(true);
        }

        public Task RemoveWatchlistTitleAsync(Guid accountId, Guid titleId, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<PagedCustomLists> GetCustomListsAsync(
            Guid accountId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedCustomLists([], page, pageSize, 0));

        public Task<CustomListData?> GetCustomListAsync(
            Guid accountId,
            Guid listId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            ReadListAccountId = accountId;
            if (accountId != ListOwnerId)
            {
                return Task.FromResult<CustomListData?>(null);
            }

            var summary = new CustomListSummary(
                listId,
                "Privada",
                null,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                0);
            return Task.FromResult<CustomListData?>(
                new CustomListData(summary, new PagedTitleList([], page, pageSize, 0)));
        }

        public Task<CustomListSummary> CreateCustomListAsync(
            Guid accountId,
            CreateCustomList request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CustomListSummary?> UpdateCustomListAsync(
            Guid accountId,
            Guid listId,
            UpdateCustomList request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteCustomListAsync(
            Guid accountId,
            Guid listId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ListItemAddResult> AddCustomListTitleAsync(
            Guid accountId,
            Guid listId,
            Guid titleId,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> RemoveCustomListTitleAsync(
            Guid accountId,
            Guid listId,
            Guid titleId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ListReorderResult> ReorderCustomListTitlesAsync(
            Guid accountId,
            Guid listId,
            IReadOnlyList<Guid> titleIds,
            CancellationToken cancellationToken) =>
            Task.FromResult(ListReorderResult.Updated);
    }
}
