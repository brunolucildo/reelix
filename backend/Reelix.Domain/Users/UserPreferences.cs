namespace Reelix.Domain.Users;

public sealed class UserPreferences
{
    private UserPreferences()
    {
    }

    private UserPreferences(Guid accountId)
    {
        AccountId = accountId;
        CurrentStep = "Welcome";
    }

    public Guid AccountId { get; private set; }

    public string CurrentStep { get; private set; } = "Welcome";

    public DateTimeOffset? CompletedAt { get; private set; }

    public ICollection<UserGenrePreference> GenrePreferences { get; private set; } =
        new List<UserGenrePreference>();

    public ICollection<UserFavoriteTitle> FavoriteTitles { get; private set; } =
        new List<UserFavoriteTitle>();

    public static UserPreferences Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe um identificador de conta válido.", nameof(accountId));
        }

        return new UserPreferences(accountId);
    }

    public void UpdateProgress(string currentStep)
    {
        if (string.IsNullOrWhiteSpace(currentStep))
        {
            throw new ArgumentException("Informe uma etapa do onboarding.", nameof(currentStep));
        }

        CurrentStep = currentStep;
    }

    public void Complete()
    {
        CompletedAt ??= DateTimeOffset.UtcNow;
        CurrentStep = "Completed";
    }
}