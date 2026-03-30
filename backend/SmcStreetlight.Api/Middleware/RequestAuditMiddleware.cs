using SmcStreetlight.Api.Data;
using SmcStreetlight.Api.Models;

namespace SmcStreetlight.Api.Middleware;

public class RequestAuditMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        await next(context);

        if (!context.Request.Path.StartsWithSegments("/api")) return;
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 500) return;

        db.ActionLogs.Add(new ActionLog
        {
            Action = "HttpRequest",
            Details = $"{context.Request.Method} {context.Request.Path} {context.Response.StatusCode}"
        });
        await db.SaveChangesAsync();
    }
}
