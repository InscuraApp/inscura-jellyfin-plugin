using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Jellyfin.Plugin.Inscura.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Inscura.Api;

public sealed class InscuraApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InscuraApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public InscuraApiClient(IHttpClientFactory httpClientFactory, ILogger<InscuraApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ApiLibrarySummary?> GetLibraryAsync(CancellationToken cancellationToken)
    {
        return await GetAsync<ApiLibrarySummary>("api/v1/library", cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ApiMediaListItem>> SearchMediaAsync(string query, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<ApiMediaListItem>();
        }

        var path = "api/v1/media/search?q=" + Uri.EscapeDataString(query.Trim()) + "&limit=" + ClampLimit(limit).ToString(CultureInfo.InvariantCulture);
        var results = await GetAsync<List<ApiMediaListItem>>(path, cancellationToken).ConfigureAwait(false);
        return results ?? new List<ApiMediaListItem>();
    }

    public async Task<ApiMediaDetail?> GetMediaAsync(long id, CancellationToken cancellationToken)
    {
        return await GetAsync<ApiMediaDetail>("api/v1/media/" + id.ToString(CultureInfo.InvariantCulture), cancellationToken).ConfigureAwait(false);
    }

    public Task<HttpResponseMessage> GetImageResponseAsync(string url, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyToken(request, GetConfiguration());
        return CreateHttpClient(GetConfiguration()).SendAsync(request, cancellationToken);
    }

    public string AddQueryTokenIfNeeded(string url)
    {
        var configuration = GetConfiguration();
        if (string.IsNullOrWhiteSpace(configuration.ApiToken) || !IsInscuraApiUrl(url, configuration))
        {
            return url;
        }

        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return url + separator + "token=" + Uri.EscapeDataString(configuration.ApiToken.Trim());
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var configuration = GetConfiguration();
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(path, configuration));
        ApplyToken(request, configuration);

        try
        {
            using var response = await CreateHttpClient(configuration).SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Inscura API request failed: {StatusCode} {Path}", response.StatusCode, path);
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var envelope = await JsonSerializer.DeserializeAsync<ApiEnvelope<T>>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            return envelope is null ? default : envelope.Data;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inscura API request failed: {Path}", path);
            return default;
        }
    }

    private HttpClient CreateHttpClient(PluginConfiguration configuration)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(configuration.RequestTimeoutSeconds, 3, 120));
        return client;
    }

    private static Uri BuildUri(string path, PluginConfiguration configuration)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuration.ApiBaseUrl)
            ? "http://127.0.0.1:28687"
            : configuration.ApiBaseUrl.Trim();

        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }

        return new Uri(new Uri(baseUrl), path);
    }

    private static void ApplyToken(HttpRequestMessage request, PluginConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ApiToken))
        {
            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiToken.Trim());
    }

    private static bool IsInscuraApiUrl(string url, PluginConfiguration configuration)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var assetUri))
        {
            return false;
        }

        var baseUrl = string.IsNullOrWhiteSpace(configuration.ApiBaseUrl)
            ? "http://127.0.0.1:28687"
            : configuration.ApiBaseUrl.Trim();

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var apiUri))
        {
            return false;
        }

        return string.Equals(assetUri.Scheme, apiUri.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(assetUri.Host, apiUri.Host, StringComparison.OrdinalIgnoreCase)
            && assetUri.Port == apiUri.Port;
    }

    private static int ClampLimit(int limit)
    {
        return Math.Clamp(limit <= 0 ? 10 : limit, 1, 50);
    }

    private static PluginConfiguration GetConfiguration()
    {
        return Plugin.Instance?.Configuration ?? new PluginConfiguration();
    }
}
