namespace Auth.Api.Middleware;

/// <summary>
/// Blocks all API requests (except login, refresh, and change-password) when the authenticated
/// user has the <c>mustChangePassword</c> claim set to <c>"true"</c>.
/// This is a defense-in-depth measure — the frontend also enforces this flow,
/// but the backend must protect against direct API calls that bypass the UI.
/// </summary>
public class MustChangePasswordMiddleware
{
    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var mustChange = context.User.FindFirst("mustChangePassword")?.Value;
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (mustChange == "true"
                && !path.EndsWith("/auth/login")
                && !path.EndsWith("/auth/refresh")
                && !path.EndsWith("/auth/change-password"))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { message = "請先修改密碼" });
                return;
            }
        }

        await _next(context);
    }
}
