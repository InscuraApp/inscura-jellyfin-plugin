using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Inscura.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        ApiBaseUrl = "http://127.0.0.1:28687";
        ApiToken = string.Empty;
        RequestTimeoutSeconds = 15;
        SearchLimit = 10;
        EnableMetadataProvider = true;
        EnableImageProvider = true;
        EnableYouTubeTrailers = true;
        IncludePreviewAsThumb = true;
        IncludeGalleryBackdrops = true;
    }

    public string ApiBaseUrl { get; set; }

    public string ApiToken { get; set; }

    public int RequestTimeoutSeconds { get; set; }

    public int SearchLimit { get; set; }

    public bool EnableMetadataProvider { get; set; }

    public bool EnableImageProvider { get; set; }

    public bool EnableYouTubeTrailers { get; set; }

    public bool IncludePreviewAsThumb { get; set; }

    public bool IncludeGalleryBackdrops { get; set; }
}
