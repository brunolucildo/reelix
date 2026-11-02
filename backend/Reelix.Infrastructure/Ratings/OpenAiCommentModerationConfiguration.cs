using Microsoft.Extensions.Options;
using Reelix.Application.Ratings;

namespace Reelix.Infrastructure.Ratings;

public sealed class OpenAiCommentModerationConfiguration(
    IOptions<OpenAiSpoilerOptions> options) : ICommentModerationConfiguration
{
    public bool IsEnabled => options.Value.Enabled && !options.Value.TemporarilyDisabled;
}
