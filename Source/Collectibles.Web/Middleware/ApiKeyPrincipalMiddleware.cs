using Collectibles.Web.Authentication;

using Microsoft.AspNetCore.Authentication;

namespace Collectibles.Web.Middleware;

/// <summary>
/// Establishes the API-key principal for requests that present <c>X-Api-Key</c>, including
/// endpoints marked <c>AllowAnonymous</c>.
/// </summary>
/// <remarks>
/// The attachment thumbnail and download endpoints are <c>AllowAnonymous</c> and authorize
/// internally from <c>HttpContext.User</c>. The API-key scheme, however, only ran through
/// the <c>ApiKeyOrCookie</c> policy, and the default authenticate scheme is the Identity
/// cookie — so the SyncTool's API key was never evaluated on those routes and every
/// private-showcase preview or download it requested came back 401.
///
/// Running the scheme here means a credential the endpoint claims to accept actually gets
/// its scheme run. Requests without the header, or already authenticated by a cookie, are
/// untouched.
/// </remarks>
public class ApiKeyPrincipalMiddleware
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;

    public ApiKeyPrincipalMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated != true
            && context.Request.Headers.ContainsKey(ApiKeyHeaderName))
        {
            var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);

            if (result.Succeeded && result.Principal is not null)
            {
                context.User = result.Principal;
            }
        }

        await _next(context);
    }
}
