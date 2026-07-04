using Jellyfin.Plugin.Inscura.Api;
using Jellyfin.Plugin.Inscura.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Inscura.Providers;

public sealed class InscuraMovieProvider : IRemoteMetadataProvider<Movie, MovieInfo>, IHasOrder
{
    private readonly InscuraApiClient _apiClient;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<InscuraMovieProvider> _logger;

    public InscuraMovieProvider(IHttpClientFactory httpClientFactory, ILibraryManager libraryManager, ILoggerFactory loggerFactory)
    {
        _apiClient = new InscuraApiClient(httpClientFactory, loggerFactory.CreateLogger<InscuraApiClient>());
        _libraryManager = libraryManager;
        _logger = loggerFactory.CreateLogger<InscuraMovieProvider>();
    }

    public string Name => Plugin.PluginName;

    public int Order => 0;

    public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MovieInfo searchInfo, CancellationToken cancellationToken)
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
            var results = await _apiClient.SearchMediaAsync(query, Configuration.SearchLimit, cancellationToken).ConfigureAwait(false);
            if (results.Count > 0)
            {
                return results.Select(item => InscuraMapping.ToSearchResult(item, Name, _apiClient)).ToArray();
            }
        }

        return Array.Empty<RemoteSearchResult>();
    }

    public async Task<MetadataResult<Movie>> GetMetadata(MovieInfo info, CancellationToken cancellationToken)
    {
        var result = new MetadataResult<Movie>
        {
            Provider = Name,
            ResultLanguage = info.MetadataLanguage
        };

        if (!Configuration.EnableMetadataProvider)
        {
            return result;
        }

        var detail = await ResolveMediaAsync(info, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return result;
        }

        var movie = InscuraMapping.ToMovie(detail);
        if (Configuration.EnableYouTubeTrailers)
        {
            movie.RemoteTrailers = InscuraMapping.GetYouTubeTrailers(detail);
        }

        result.HasMetadata = true;
        result.Item = movie;
        IReadOnlyList<PersonInfo> people = InscuraMapping.GetPeople(detail, _apiClient).ToArray();
        result.People = people;
        return result;
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _apiClient.GetImageResponseAsync(url, cancellationToken);
    }

    private async Task<RemoteSearchResult?> TryGetSearchResultByProviderId(MovieInfo searchInfo, CancellationToken cancellationToken)
    {
        if (!TryGetInscuraId(searchInfo.ProviderIds, out var id))
        {
            return null;
        }

        var detail = await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return null;
        }

        var listItem = new ApiMediaListItem
        {
            Id = detail.Id,
            Title = InscuraMapping.GetMeta(detail, "title"),
            Code = InscuraMapping.GetMeta(detail, "code"),
            DurationMs = detail.DurationMs,
            Width = detail.Width,
            Height = detail.Height,
            FileName = detail.FileName,
            RelativePath = detail.RelativePath,
            Assets = detail.Assets
        };
        return InscuraMapping.ToSearchResult(listItem, Name, _apiClient);
    }

    private async Task<ApiMediaDetail?> ResolveMediaAsync(MovieInfo info, CancellationToken cancellationToken)
    {
        if (TryGetInscuraId(info.ProviderIds, out var id))
        {
            return await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
        }

        var queries = GetSearchQueries(info);
        if (queries.Count == 0)
        {
            return null;
        }

        foreach (var query in queries)
        {
            var results = await _apiClient.SearchMediaAsync(query, 1, cancellationToken).ConfigureAwait(false);
            var first = results.FirstOrDefault();
            if (first is not null)
            {
                return await _apiClient.GetMediaAsync(first.Id, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    private IReadOnlyList<string> GetSearchQueries(MovieInfo info)
    {
        var queries = InscuraMapping.GetSearchQueries(info.Path, info.Name, name => _libraryManager.ParseName(name).Name);
        if (queries.Count == 0)
        {
            _logger.LogDebug("No Inscura search query can be derived for movie lookup.");
        }

        return queries;
    }

    private static bool TryGetInscuraId(IReadOnlyDictionary<string, string> providerIds, out long id)
    {
        id = 0;
        return providerIds.TryGetValue(Plugin.ProviderId, out var value)
            && long.TryParse(value, out id)
            && id > 0;
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();
}
