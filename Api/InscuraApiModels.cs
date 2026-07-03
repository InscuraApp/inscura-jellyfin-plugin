using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Inscura.Api;

public sealed class ApiEnvelope<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class ApiLibrarySummary
{
    [JsonPropertyName("libraryId")]
    public string? LibraryId { get; set; }

    [JsonPropertyName("libraryName")]
    public string? LibraryName { get; set; }

    [JsonPropertyName("baseUrl")]
    public string? BaseUrl { get; set; }
}

public sealed class ApiMediaPage
{
    [JsonPropertyName("list")]
    public List<ApiMediaListItem> List { get; set; } = new();
}

public sealed class ApiMediaListItem
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("relativePath")]
    public string? RelativePath { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("assets")]
    public List<ApiMediaAsset> Assets { get; set; } = new();
}

public sealed class ApiMediaDetail
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("relativePath")]
    public string? RelativePath { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("metas")]
    public Dictionary<string, string?> Metas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("terms")]
    public Dictionary<string, List<ApiTerm>> Terms { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("credits")]
    public ApiCredits Credits { get; set; } = new();

    [JsonPropertyName("assets")]
    public List<ApiMediaAsset> Assets { get; set; } = new();
}

public sealed class ApiMediaAsset
{
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("sourceUrl")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("remoteUrl")]
    public string? RemoteUrl { get; set; }
}

public sealed class ApiTerm
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("typeName")]
    public string? TypeName { get; set; }
}

public sealed class ApiCredits
{
    [JsonPropertyName("cast")]
    public List<ApiCredit> Cast { get; set; } = new();

    [JsonPropertyName("crew")]
    public List<ApiCredit> Crew { get; set; } = new();
}

public sealed class ApiCredit
{
    [JsonPropertyName("actorId")]
    public long ActorId { get; set; }

    [JsonPropertyName("actorName")]
    public string? ActorName { get; set; }

    [JsonPropertyName("sortOrder")]
    public int? SortOrder { get; set; }

    [JsonPropertyName("roles")]
    public List<ApiRole> Roles { get; set; } = new();

    [JsonPropertyName("actorAvatar")]
    public ApiMediaAsset? ActorAvatar { get; set; }
}

public sealed class ApiRole
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
