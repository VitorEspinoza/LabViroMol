using System.Net;
using System.Net.Http.Headers;
namespace LabViroMol.LoadTests.Infrastructure;

public static class HttpClientFactory
{
    public static HttpClient Create(LoadTestConfig config)
    {
        var handler = new SocketsHttpHandler
        {
            UseCookies = false,
            UseProxy = false,
            AutomaticDecompression = DecompressionMethods.All,
            EnableMultipleHttp2Connections = true,
            PooledConnectionLifetime = TimeSpan.FromMinutes(config.PooledConnectionLifetimeMinutes),
            MaxConnectionsPerServer = config.MaxConnectionsPerServer,
            SslOptions =
            {
                RemoteCertificateValidationCallback = (_, _, _, _) => config.AllowInsecureTls
            }
        };

        if (!config.KeepAliveEnabled)
        {
            handler.PooledConnectionLifetime = TimeSpan.FromMilliseconds(1);
        }

        var client = new HttpClient(handler);
        client.BaseAddress = new Uri(config.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!config.KeepAliveEnabled)
        {
            client.DefaultRequestHeaders.ConnectionClose = true;
        }

        return client;
    }
}
