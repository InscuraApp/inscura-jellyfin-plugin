using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Inscura.Providers;

public sealed class InscuraMovieExternalId : IExternalId
{
    public string ProviderName => Plugin.PluginName;

    public string Key => Plugin.ProviderId;

    public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    public string? UrlFormatString => null;

    public bool Supports(IHasProviderIds item)
    {
        return item is Movie;
    }
}

public sealed class InscuraCodeExternalId : IExternalId
{
    public string ProviderName => "Inscura Code";

    public string Key => Plugin.CodeProviderId;

    public ExternalIdMediaType? Type => ExternalIdMediaType.Movie;

    public string? UrlFormatString => null;

    public bool Supports(IHasProviderIds item)
    {
        return item is Movie;
    }
}

public sealed class InscuraPersonExternalId : IExternalId
{
    public string ProviderName => "Inscura Actor";

    public string Key => Plugin.PersonProviderId;

    public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

    public string? UrlFormatString => null;

    public bool Supports(IHasProviderIds item)
    {
        return item is Person;
    }
}
