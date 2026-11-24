namespace Reelix.Domain.Notifications;

/// <summary>
/// The timing rules of the push delivery: lease, retry backoff, attempt limit and receipt
/// checks. Pure: the randomness of the jitter comes in as a parameter.
/// </summary>
public static class PushRetryPolicy
{
    /// <summary>How long a claimed delivery stays reserved for one worker.</summary>
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    /// <summary>How long after a ticket is stored its receipt is first checked.</summary>
    public static readonly TimeSpan FirstReceiptCheckDelay = TimeSpan.FromMinutes(15);

    /// <summary>How long a ticket waits for a receipt before it expires.</summary>
    public static readonly TimeSpan ReceiptExpiry = TimeSpan.FromHours(24);

    private const double BaseRetrySeconds = 5;
    private const double MaxRetrySeconds = 3600;
    private const double JitterSpread = 0.4;
    private const double JitterFloor = 0.8;

    /// <summary>
    /// The wait before the next attempt: the provider hint when there is one, otherwise
    /// <c>5 * 2^(attempt - 1)</c> seconds (at most an hour) varied by +-20 %.
    /// </summary>
    /// <param name="attemptCount">Attempts made so far, including the one that just failed.</param>
    /// <param name="retryAfter">The wait the provider asked for, when it did.</param>
    /// <param name="jitter">A random number in [0, 1) that spreads retries apart.</param>
    public static TimeSpan NextSendDelay(int attemptCount, TimeSpan? retryAfter, double jitter)
    {
        if (retryAfter is { } hint)
        {
            return hint;
        }

        var exponentialSeconds = Math.Min(
            MaxRetrySeconds,
            BaseRetrySeconds * Math.Pow(2, Math.Min(attemptCount - 1, 10)));
        return TimeSpan.FromSeconds(exponentialSeconds * (JitterFloor + (jitter * JitterSpread)));
    }

    /// <summary>
    /// The attempt limit of a failed send: a device that is gone or an error that cannot be
    /// retried gets a single attempt, any other failure the configured limit.
    /// </summary>
    public static int MaxAttemptsAfterFailure(
        bool deviceNotRegistered,
        bool retryable,
        int configuredMaxAttempts) =>
        deviceNotRegistered || !retryable ? 1 : configuredMaxAttempts;

    /// <summary>The wait before the next receipt check: <c>15 * 2^min(checks, 5)</c> minutes, at most 6 hours.</summary>
    public static TimeSpan ReceiptCheckDelay(int checkCount) =>
        TimeSpan.FromMinutes(Math.Min(360, 15 * Math.Pow(2, Math.Min(checkCount, 5))));

    /// <summary>Whether a ticket stored at <paramref name="ticketCreatedAt"/> waited too long for its receipt.</summary>
    public static bool IsReceiptExpired(DateTimeOffset? ticketCreatedAt, DateTimeOffset now) =>
        ticketCreatedAt <= now - ReceiptExpiry;
}
