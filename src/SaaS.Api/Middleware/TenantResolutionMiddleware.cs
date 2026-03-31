using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SaaS.Domain;
using SaaS.Infrastructure.Persistence;

namespace SaaS.Api.Middleware;

/// <summary>
/// Resolves the current tenant from the request.
/// Dev: reads the <c>X-Tenant-Id</c> header.
/// Prod-style: reads the <c>TenantId</c> claim from the JWT.
/// Returns 400 ProblemDetails when the tenant cannot be resolved.
/// </summary>
public sealed class TenantResolutionMiddleware
{
    private const string HeaderName = "X-Tenant-Id";
    private const string ClaimType = "TenantId";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip tenant resolution for health-check and Swagger endpoints.
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/healthz", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 1. Try X-Tenant-Id header (dev / integration tests).
        string? raw = context.Request.Headers[HeaderName].FirstOrDefault();

        // 2. Fallback to JWT claim.
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = context.User?.FindFirstValue(ClaimType);
        }

        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var guid))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing or invalid tenant identifier",
                Detail = $"Provide a valid GUID via the '{HeaderName}' header or the '{ClaimType}' JWT claim."
            }, context.RequestAborted);
            return;
        }

        var tenantContext = context.RequestServices.GetRequiredService<TenantContext>();
        tenantContext.SetTenantId(new TenantId(guid));

        await _next(context);
    }
}
