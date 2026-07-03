using Jellyfin.Plugin.Inscura.Api;
using Jellyfin.Plugin.Inscura.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Inscura.Providers;

public sealed class InscuraMovieImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly InscuraApiClient _apiClient;

    public InscuraMovieImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _apiClient = new InscuraApiClient(httpClientFactory, loggerFactory.CreateLogger<InscuraApiClient>());
    }

    public string Name => Plugin.PluginName;

    public int Order => 0;

    public bool Supports(BaseItem item)
    {
        return item is Movie && Configuration.EnableImageProvider;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[]
        {
            ImageType.Primary,
            ImageType.Backdrop,
            ImageType.Thumb,
            ImageType.Banner,
            ImageType.Logo,
            ImageType.Art,
            ImageType.Disc
        };
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!Configuration.EnableImageProvider || item is not Movie movie)
        {
            return Array.Empty<RemoteImageInfo>();
        }

        var detail = await ResolveDetailAsync(movie, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return Array.Empty<RemoteImageInfo>();
        }

        return detail.Assets
            .Select(asset => ToRemoteImage(asset, detail))
            .Where(image => image is not null)
            .Cast<RemoteImageInfo>()
            .ToArray();
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _apiClient.GetImageResponseAsync(url, cancellationToken);
    }

    private async Task<ApiMediaDetail?> ResolveDetailAsync(Movie movie, CancellationToken cancellationToken)
    {
        if (movie.ProviderIds.TryGetValue(Plugin.ProviderId, out var value) && long.TryParse(value, out var id) && id > 0)
        {
            return await _apiClient.GetMediaAsync(id, cancellationToken).ConfigureAwait(false);
        }

        foreach (var query in InscuraMapping.GetSearchQueries(movie.Path, movie.Name, null))
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

    private RemoteImageInfo? ToRemoteImage(ApiMediaAsset asset, ApiMediaDetail detail)
    {
        var type = InscuraMapping.MapImageType(asset, Configuration.IncludePreviewAsThumb, Configuration.IncludeGalleryBackdrops);
        if (type is null || string.IsNullOrWhiteSpace(asset.Url))
        {
            return null;
        }

        return new RemoteImageInfo
        {
            ProviderName = Name,
            Url = _apiClient.AddQueryTokenIfNeeded(asset.Url),
            ThumbnailUrl = _apiClient.AddQueryTokenIfNeeded(asset.Url),
            Type = type.Value,
            Language = string.Empty,
            Width = type.Value == ImageType.Backdrop || type.Value == ImageType.Thumb ? detail.Width : null,
            Height = type.Value == ImageType.Backdrop || type.Value == ImageType.Thumb ? detail.Height : null
        };
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();
}
