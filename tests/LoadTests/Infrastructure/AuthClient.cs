using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using LabViroMol.Modules.Identity.Application.Users.Login;

namespace LabViroMol.LoadTests.Infrastructure;

public sealed class AuthClient
{
    private static readonly Regex AccessTokenRegex = new(@"^X-Access-Token=(?<token>[^;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly HttpClient _httpClient;
    private readonly LoadTestConfig _config;

    public AuthClient(HttpClient httpClient, LoadTestConfig config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<IReadOnlyList<string>> LoginAllAsync(CancellationToken ct)
    {
        var tokens = new List<string>(_config.Auth.UserCount);

        for (var i = 0; i < _config.Auth.UserCount; i++)
        {
            var email = $"{_config.Auth.EmailPrefix}{i + 1}@test.local";
            tokens.Add(await LoginAsync(email, _config.Auth.Password, ct));
        }

        return tokens;
    }

    private async Task<string> LoginAsync(string email, string password, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/identity/users/login", new LoginCommand(email, password), ct);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Falha ao autenticar usuário '{email}'. Status: {(int)response.StatusCode}. Corpo: {body}");
        }

        if (!response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
            throw new InvalidOperationException($"Login de '{email}' não retornou Set-Cookie.");

        foreach (var cookieHeader in cookieHeaders)
        {
            var match = AccessTokenRegex.Match(cookieHeader);
            if (match.Success)
                return match.Groups["token"].Value;
        }

        throw new InvalidOperationException($"Login de '{email}' não retornou o cookie X-Access-Token.");
    }
}
