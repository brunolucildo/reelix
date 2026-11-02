namespace Reelix.Infrastructure.Ratings;

public sealed class OpenAiSpoilerOptions
{
    public const string SectionName = "OpenAI:SpoilerAnalysis";

    public bool Enabled { get; set; }

    public bool TemporarilyDisabled { get; set; } = true;

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4o-mini";

    public int TimeoutSeconds { get; set; } = 10;
}
