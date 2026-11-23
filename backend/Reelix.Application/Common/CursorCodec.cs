using System.Text;
using System.Text.Json;

namespace Reelix.Application.Common;

/// <summary>
/// The single opaque-cursor codec: base64url without padding, at most 200 characters. Flows
/// encode either a JSON payload (social feed, notifications inbox) or a plain text payload
/// (monthly ranking); each module keeps its own invalid-cursor message.
/// </summary>
public static class CursorCodec
{
    public const int MaxLength = 200;

    public static string EncodeJson<T>(T value) =>
        ToBase64Url(JsonSerializer.SerializeToUtf8Bytes(value));

    public static string EncodeText(string value) =>
        ToBase64Url(Encoding.UTF8.GetBytes(value));

    /// <summary>
    /// Decodes a non-blank cursor into <typeparamref name="T"/>. The result can be null when the
    /// payload is the JSON literal null; the caller validates the content.
    /// </summary>
    /// <param name="invalid">
    /// Builds the module-specific error; receives the decoding failure, or null when the cursor is
    /// simply too long.
    /// </param>
    public static T? DecodeJson<T>(string encoded, Func<Exception?, ArgumentException> invalid)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(encoded);
        ArgumentNullException.ThrowIfNull(invalid);
        if (encoded.Length > MaxLength)
        {
            throw invalid(null);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(FromBase64Url(encoded));
        }
        catch (FormatException exception)
        {
            throw invalid(exception);
        }
        catch (JsonException exception)
        {
            throw invalid(exception);
        }
    }

    /// <summary>Decodes a text cursor; false when it is too long or not valid base64url.</summary>
    public static bool TryDecodeText(string encoded, out string text)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        text = string.Empty;
        if (encoded.Length > MaxLength)
        {
            return false;
        }

        try
        {
            text = Encoding.UTF8.GetString(FromBase64Url(encoded));
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string encoded)
    {
        var value = encoded.Replace('-', '+').Replace('_', '/');
        value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
        return Convert.FromBase64String(value);
    }
}
