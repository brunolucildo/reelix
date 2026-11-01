namespace Reelix.Api;

internal static class ArgumentExceptionExtensions
{
    // ArgumentException.Message appends " (Parameter 'name')", which must not reach end users.
    public static string ToUserMessage(this ArgumentException exception) =>
        exception.ParamName is null
            ? exception.Message
            : exception.Message.Replace(
                $" (Parameter '{exception.ParamName}')",
                string.Empty,
                StringComparison.Ordinal);
}
