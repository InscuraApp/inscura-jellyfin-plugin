using Jellyfin.Plugin.Inscura.Api;
using Jellyfin.Plugin.Inscura.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Inscura.Providers;

public sealed class InscuraPersonImageProvider : IRemoteImageProvider, IHasOrder
{
    private readonly InscuraApiClient _apiClient;

    public InscuraPersonImageProvider(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _apiClient = new InscuraApiClient(httpClientFactory, loggerFactory.CreateLogger<InscuraApiClient>());
    }

    public string Name => Plugin.PluginName;

    public int Order => 0;

    public bool Supports(BaseItem item)
    {
        return item is Person && Configuration.EnableImageProvider;
    }

    public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
    {
        return new[]
        {
            ImageType.Primary,
            ImageType.Profile
        };
    }

    public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
    {
        if (!Configuration.EnableImageProvider || item is not Person person)
        {
            return Array.Empty<RemoteImageInfo>();
        }

        var detail = await ResolveDetailAsync(person, cancellationToken).ConfigureAwait(false);
        if (detail is null)
        {
            return Array.Empty<RemoteImageInfo>();
        }

        return detail.Assets
            .Select(ToRemoteImage)
            .Where(image => image is not null)
            .Cast<RemoteImageInfo>()
            .ToArray();
    }

    public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
    {
        return _apiClient.GetImageResponseAsync(url, cancellationToken);
    }

    private async Task<ApiActorDetail?> ResolveDetailAsync(Person person, CancellationToken cancellationToken)
    {
        if (person.ProviderIds.TryGetValue(Plugin.PersonProviderId, out var value) && long.TryParse(value, out var id) && id > 0)
        {
            return await _apiClient.GetActorAsync(id, cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(person.Name))
        {
            return null;
        }

        var results = await _apiClient.SearchActorsAsync(person.Name, 1, cancellationToken).ConfigureAwait(false);
        var first = results.FirstOrDefault();
        return first is null ? null : await _apiClient.GetActorAsync(first.Id, cancellationToken).ConfigureAwait(false);
    }

    private RemoteImageInfo? ToRemoteImage(ApiMediaAsset asset)
    {
        var type = InscuraMapping.MapActorImageType(asset);
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
            Language = string.Empty
        };
    }

    private static PluginConfiguration Configuration => Plugin.Instance?.Configuration ?? new PluginConfiguration();
}
