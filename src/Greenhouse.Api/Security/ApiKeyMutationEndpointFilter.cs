using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Greenhouse.Api.Security;

public sealed class ApiKeyMutationEndpointFilter : IEndpointFilter
{
    private readonly IOptions<ApiKeyOptions> _options;

    public ApiKeyMutationEndpointFilter(IOptions<ApiKeyOptions> options)
    {
        _options = options;
    }

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var opts = _options.Value;
        if (!opts.RequireForMutations)
        {
            return next(context);
        }

        var http = context.HttpContext;
        if (!http.Request.Headers.TryGetValue(opts.HeaderName, out var supplied))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        var expected = opts.ApiKey?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(expected) || !string.Equals(supplied.ToString(), expected, StringComparison.Ordinal))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        return next(context);
    }
}
