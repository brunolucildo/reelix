namespace Reelix.Api.Errors;

/// <summary>
/// Endpoint filter that turns an expected application failure into a problem response. It is a
/// filter and not an <c>IExceptionHandler</c> on purpose (ADR XXXX-0013): the host exception
/// middleware logs every exception that reaches it at Error level, which would turn each 400 or
/// 404 into noise, while the filter only catches the typed failures and logs nothing.
/// </summary>
internal sealed class ApplicationProblemFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception exception) when (ApplicationProblemMapper.CanMap(exception))
        {
            var options = context.HttpContext.GetEndpoint()?.Metadata
                .GetMetadata<ApplicationProblemOptions>() ?? new ApplicationProblemOptions();
            return ApplicationProblemMapper.Map(exception, options, context.HttpContext.Response);
        }
    }
}
