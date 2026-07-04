using Jellyfin.Plugin.Inscura.Api;
using Jellyfin.Plugin.Inscura.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Inscura.Providers;

public sealed class InscuraPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>, IHasOrder
{
    private readonly InscuraApiClient _apiClient;
    private readonly ILogger<InscuraPersonProvider> _logger;

    public InscuraPersonProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _apiClient = new InscuraApiClient(httpClientFactory, loggerFactory.CreateLogger<InscuraApiClient>());
        _logger = loggerFactory.CreateLogger<InscuraPersonProvider>();
    }

    public string Name => Plugin.PluginName;

    public int Order => 0;

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        if (!Configuration.EnableMetadataProvider)
        {
            return Array.Empty<RemoteSearchResult>();
        }

        var byId = await TryGetSearchResultByProviderId(searchInfo, cancellationToken).ConfigureAwait(false);
        if (byId is not null)
        {
            return new[] { byId };
        }

        var queries = GetSearchQueries(searchInfo);
        if (queries.Count == 0)
        {
            return Array.Empty<RemoteSearchResult>();
        }

        foreach (var query in queries)
        {
            var results = await _apiClient.SearchActorsAsync(query, Configuration.SearchLimit, cancellationToken).ConfigureAwait(false);
            if (results.Count > 0)
            {
                return results.Select(item => InscuraMapping.ToActorSearchResult(item, Name, _apiClient)).ToArray();
            }
        }

        return Array.Empty<RemoteSearchResult>();
    }

    public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Person>
        {
            Provider = Name,
            ResultLanguage = info.MetadataLanguage
        };

        if (!Configuration.EnableMetadataProvider)
        {
            return result;
        }

        var detail = await ResolveActorAsync(info, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return result;
        }

        result.HasMetadata = true;
        result.Item = InscuraMapping.ToPerson(detail);
        result.QueriedById = TryGetInscuraActorId(info.ProviderIds, out _);
        return result;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _apiClient.GetImageResponseAsync(url, cancellationToken);
    }

    private async Task<RemoteSearchResult?> TryGetSearchResultByProviderId(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
    {
        if (!TryGetInscuraActorId(searchInfo.ProviderIds, out var id))
        {
            return null;
        }

        var detail = await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
        return detail is null ? null : InscuraMapping.ToActorSearchResult(detail, Name, _apiClient);
    }

    private async Task<ApiActorDetail?> ResolveActorAsync(PersonLookupInfo info, CancellationToken cancellationToken)
    {
        if (TryGetInscuraActorId(info.ProviderIds, out var id))
        {
            return await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var queries = GetSearchQueries(info);
        if (queries.Count == 0)
        {
            return null;
        }

        foreach (var query in queries)
        {
            var results = await _apiClient.SearchActorsAsync(query, 1, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            if (first is not null)
            {
                return await _apiClient.GetActorAsync(first.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    private IReadOnlyList<string> GetSearchQueries(PersonLookupInfo info)
    {
        var queries = new List<string>();
        AddQuery(queries, info.Name);
        AddQuery(queries, info.OriginalTitle);

        if (queries.Count == 0)
        {
            _logger.LogDebug("No Inscura search query can be derived for person lookup.");
        }

        return queries;
    }

    private static void AddQuery(ICollection<string> queries, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var query = value.Trim();
        if (!queries.Contains(query, StringComparer.OrdinalIgnoreCase))
        {
            queries.Add(query);
        }
    }

    private static bool TryGetInscuraActorId(IReadOnlyDictionary<string, string> providerIds, out long id)
    {
        id = 0;
        return providerIds.TryGetValue(Plugin.PersonProviderId, out var value)
            && long.TryParse(value, out id)
            && id > 0;
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();
}
