namespace Reelix.UnitTests.Common;

/// <summary>
/// A hand-written clock for deterministic time in tests (spec 0013 forbids new packages, so no
/// <c>TimeProvider.Testing</c>): it stays on one instant until the test moves it.
/// </summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    /// <summary>An arbitrary but stable instant for tests that do not care about the date.</summary>
    public static DateTimeOffset DefaultNow { get; } = new(2030, 6, 15, 10, 20, 30, TimeSpan.Zero);

    public static FixedTimeProvider AtDefault() => new(DefaultNow);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan amount) => _now += amount;

    public void Set(DateTimeOffset now) => _now = now;
}
