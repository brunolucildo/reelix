namespace Reelix.Domain.Gamification;

public enum AchievementRuleType
{
    MovieCount,
    EpisodeCount,
    SeriesCount,
    MovieGenreCount,
}

public sealed class AchievementDefinition
{
    private AchievementDefinition()
    {
    }

    private AchievementDefinition(
        Guid id,
        string code,
        string title,
        string description,
        AchievementRuleType ruleType,
        int threshold,
        int version)
    {
        Id = id;
        Code = code;
        Title = title;
        Description = description;
        RuleType = ruleType;
        Threshold = threshold;
        Version = version;
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public AchievementRuleType RuleType { get; private set; }

    public int Threshold { get; private set; }

    public int Version { get; private set; }

    public bool IsActive { get; private set; }

    public static AchievementDefinition Create(
        Guid id,
        string code,
        string title,
        string description,
        AchievementRuleType ruleType,
        int threshold,
        int version)
    {
        if (id == Guid.Empty ||
            string.IsNullOrWhiteSpace(code) || code.Length > 60 ||
            string.IsNullOrWhiteSpace(title) || title.Length > 100 ||
            string.IsNullOrWhiteSpace(description) || description.Length > 240 ||
            !Enum.IsDefined(ruleType) ||
            threshold <= 0 ||
            version <= 0)
        {
            throw new ArgumentException("A definição da conquista é inválida.");
        }

        return new AchievementDefinition(
            id,
            code.Trim(),
            title.Trim(),
            description.Trim(),
            ruleType,
            threshold,
            version);
    }

    public static IReadOnlyList<AchievementDefinition> InitialCatalog { get; } =
    [
        Create(
            Guid.Parse("09000000-0000-0000-0000-000000000001"),
            "first-movie",
            "Primeiro filme",
            "Registre seu primeiro filme assistido.",
            AchievementRuleType.MovieCount,
            1,
            1),
        Create(
            Guid.Parse("09000000-0000-0000-0000-000000000002"),
            "cinephile",
            "Cinéfilo",
            "Registre 10 filmes distintos como assistidos.",
            AchievementRuleType.MovieCount,
            10,
            1),
        Create(
            Guid.Parse("09000000-0000-0000-0000-000000000003"),
            "first-marathon",
            "Maratona inicial",
            "Registre 10 episódios distintos como assistidos.",
            AchievementRuleType.EpisodeCount,
            10,
            1),
        Create(
            Guid.Parse("09000000-0000-0000-0000-000000000004"),
            "series-in-focus",
            "Séries em foco",
            "Assista a pelo menos um episódio em três séries distintas.",
            AchievementRuleType.SeriesCount,
            3,
            1),
        Create(
            Guid.Parse("09000000-0000-0000-0000-000000000005"),
            "genre-explorer",
            "Explorador de gêneros",
            "Assista a filmes de cinco gêneros canônicos distintos.",
            AchievementRuleType.MovieGenreCount,
            5,
            1),
    ];
}
