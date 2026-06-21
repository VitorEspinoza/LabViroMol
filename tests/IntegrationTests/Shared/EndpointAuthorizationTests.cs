using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.IntegrationTests.Shared;

public sealed class EndpointAuthorizationTests : IClassFixture<LabViroMolWebAppFactory>
{
    private static readonly string[] AllowedAnonymousEndpoints =
    [
        "GET /api/assets/public/equipments",
        "GET /api/assets/public/equipments/{id:guid}",
        "GET /api/assets/public/equipments/schedulable",
        "GET /api/research/public/researchers",
        "GET /api/research/public/publications",
        "GET /api/research/public/projects",
        "GET /api/research/public/partners",
        "GET /api/scheduling/public/schedules",
        "POST /api/identity/users/login",
        "POST /api/identity/users/refresh",
        "POST /api/identity/users/forgot-password",
        "POST /api/identity/users/reset-password",
    ];

    private readonly LabViroMolWebAppFactory _factory;

    public EndpointAuthorizationTests(LabViroMolWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Every_route_endpoint_requires_authorization_or_is_in_the_explicit_public_allowlist()
    {
        using var scope = _factory.Services.CreateScope();

        var unauthorizedEndpoints = scope.ServiceProvider
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is not null)
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAuthorizeData>() is null)
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .SelectMany(GetEndpointKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(endpoint => !AllowedAnonymousEndpoints.Contains(endpoint, StringComparer.OrdinalIgnoreCase))
            .OrderBy(endpoint => endpoint, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.True(
            unauthorizedEndpoints.Length == 0,
            $"Endpoints without authorization metadata or allowlist entry:{Environment.NewLine}{string.Join(Environment.NewLine, unauthorizedEndpoints)}");
    }

    private static IEnumerable<string> GetEndpointKeys(RouteEndpoint endpoint)
    {
        var route = endpoint.RoutePattern.RawText!;
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;

        if (methods is null || methods.Count == 0)
        {
            yield return $"* {route}";
            yield break;
        }

        foreach (var method in methods)
        {
            yield return $"{method.ToUpperInvariant()} {route}";
        }
    }
}
