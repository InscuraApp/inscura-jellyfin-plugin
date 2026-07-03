using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.Inscura.Api;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Inscura.Providers;

internal static partial class InscuraMapping
{
    private static readonly string[] HomePageKeys =
    {
        "source_url",
        "homepage",
        "plugin:inscura-plugin-metatube:FANZA:homepage"
    };

    public static RemoteSearchResult ToSearchResult(ApiMediaListItem item, string providerName, InscuraApiClient client)
    {
        var result = new RemoteSearchResult
        {
            Name = FirstNonEmpty(item.Title, item.Code, TrimExtension(item.FileName), item.RelativePath),
            ProductionYear = null,
            ImageUrl = client.AddQueryTokenIfNeeded(GetFirstAssetUrl(item.Assets, "poster")),
            SearchProviderName = providerName
        };

        result.ProviderIds[Plugin.ProviderId] = item.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(item.Code))
        {
            result.ProviderIds[Plugin.CodeProviderId] = item.Code.Trim();
        }

        return result;
    }

    public static Movie ToMovie(ApiMediaDetail detail)
    {
        var title = GetMeta(detail, "title");
        var code = GetMeta(detail, "code");
        var movie = new Movie
        {
            Name = FirstNonEmpty(title, code, TrimExtension(detail.FileName), detail.RelativePath),
            OriginalTitle = FirstNonEmpty(title, code),
            Overview = GetMeta(detail, "description"),
            HomePageUrl = FirstMeta(detail, HomePageKeys),
            ProductionYear = ParseYear(GetMeta(detail, "release_date")),
            PremiereDate = ParseDate(GetMeta(detail, "release_date")),
            CommunityRating = ParseFloat(GetMeta(detail, "rating")),
            RunTimeTicks = detail.DurationMs.HasValue ? detail.DurationMs.Value * TimeSpan.TicksPerMillisecond : null
        };

        movie.ProviderIds[Plugin.ProviderId] = detail.Id.ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(code))
        {
            movie.ProviderIds[Plugin.CodeProviderId] = code.Trim();
            movie.Tags = new[] { code.Trim() };
        }

        movie.Genres = GetTermNames(detail, "genre").ToArray();
        movie.Studios = GetTermNames(detail, "studio", "label", "maker").ToArray();
        movie.ProductionLocations = GetTermNames(detail, "country", "location").ToArray();

        return movie;
    }

    public static IEnumerable<PersonInfo> GetPeople(ApiMediaDetail detail, InscuraApiClient client)
    {
        foreach (var credit in detail.Credits.Cast.OrderBy(credit => credit.SortOrder ?? int.MaxValue))
        {
            if (string.IsNullOrWhiteSpace(credit.ActorName))
            {
                continue;
            }

            var person = new PersonInfo
            {
                Name = credit.ActorName.Trim(),
                Role = string.Join(", ", credit.Roles.Select(role => role.Name).Where(value => !string.IsNullOrWhiteSpace(value))),
                Type = PersonType.Actor,
                SortOrder = credit.SortOrder,
                ImageUrl = client.AddQueryTokenIfNeeded(credit.ActorAvatar?.Url ?? string.Empty)
            };
            person.ProviderIds[Plugin.PersonProviderId] = credit.ActorId.ToString(CultureInfo.InvariantCulture);
            yield return person;
        }

        foreach (var credit in detail.Credits.Crew.OrderBy(credit => credit.SortOrder ?? int.MaxValue))
        {
            if (string.IsNullOrWhiteSpace(credit.ActorName))
            {
                continue;
            }

            var roles = credit.Roles.Select(role => role.Name).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!).ToArray();
            var person = new PersonInfo
            {
                Name = credit.ActorName.Trim(),
                Role = string.Join(", ", roles),
                Type = MapPersonKind(roles),
                SortOrder = credit.SortOrder,
                ImageUrl = client.AddQueryTokenIfNeeded(credit.ActorAvatar?.Url ?? string.Empty)
            };
            person.ProviderIds[Plugin.PersonProviderId] = credit.ActorId.ToString(CultureInfo.InvariantCulture);
            yield return person;
        }
    }

    public static IReadOnlyList<MediaUrl> GetYouTubeTrailers(ApiMediaDetail detail)
    {
        var urls = new List<string>();
        foreach (var asset in detail.Assets.Where(asset => string.Equals(asset.Kind, "trailer", StringComparison.OrdinalIgnoreCase)))
        {
            AddIfYouTube(urls, asset.RemoteUrl);
            AddIfYouTube(urls, asset.SourceUrl);
            AddIfYouTube(urls, asset.Url);
        }

        AddTrailerMetaIfYouTube(detail, urls);

        return urls
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((url, index) => new MediaUrl
            {
                Url = url,
                Name = index == 0 ? "Trailer" : "Trailer " + (index + 1).ToString(CultureInfo.InvariantCulture)
            })
            .ToArray();
    }

    public static ImageType? MapImageType(ApiMediaAsset asset, bool includePreviewAsThumb, bool includeGalleryBackdrops)
    {
        if (string.IsNullOrWhiteSpace(asset.Kind))
        {
            return null;
        }

        return asset.Kind.Trim().ToLowerInvariant() switch
        {
            "poster" => ImageType.Primary,
            "fanart" when includeGalleryBackdrops => ImageType.Backdrop,
            "screenshot" when includeGalleryBackdrops => ImageType.Backdrop,
            "keyart" => ImageType.Art,
            "landscape" => ImageType.Thumb,
            "preview" when includePreviewAsThumb => ImageType.Thumb,
            "banner" => ImageType.Banner,
            "clearlogo" => ImageType.Logo,
            "clearart" => ImageType.Art,
            "discart" => ImageType.Disc,
            _ => null
        };
    }

    public static string ExtractSearchCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var match = CodeRegex().Match(value);
        return match.Success ? match.Value.Replace("_", "-", StringComparison.Ordinal).Replace(" ", "-", StringComparison.Ordinal).ToUpperInvariant() : string.Empty;
    }

    public static IReadOnlyList<string> GetSearchQueries(string? path, string? name, Func<string, string?>? parseName)
    {
        var queries = new List<string>();

        var fileName = GetFileName(path);
        var fileNameWithoutExtension = TrimExtension(fileName);
        AddQuery(queries, fileNameWithoutExtension);
        AddQuery(queries, ExtractSearchCode(fileNameWithoutExtension));
        AddQuery(queries, fileName);

        AddQuery(queries, ExtractSearchCode(name));
        if (!string.IsNullOrWhiteSpace(name) && parseName is not null)
        {
            AddQuery(queries, parseName(name));
        }

        AddQuery(queries, name);
        return queries;
    }

    public static string GetMeta(ApiMediaDetail detail, string key)
    {
        return detail.Metas.TryGetValue(key, out var value) ? value ?? string.Empty : string.Empty;
    }

    public static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetTermNames(ApiMediaDetail detail, params string[] names)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in names)
        {
            if (!detail.Terms.TryGetValue(key, out var terms))
            {
                continue;
            }

            foreach (var term in terms)
            {
                if (!string.IsNullOrWhiteSpace(term.Name) && set.Add(term.Name.Trim()))
                {
                    yield return term.Name.Trim();
                }
            }
        }
    }

    private static string FirstMeta(ApiMediaDetail detail, IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            var value = GetMeta(detail, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string GetFirstAssetUrl(IEnumerable<ApiMediaAsset> assets, string kind)
    {
        return assets.FirstOrDefault(asset => string.Equals(asset.Kind, kind, StringComparison.OrdinalIgnoreCase))?.Url ?? string.Empty;
    }

    private static string TrimExtension(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        return Path.GetFileNameWithoutExtension(fileName.Trim());
    }

    private static string GetFileName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Trim().Replace('\\', '/');
        var separatorIndex = normalized.LastIndexOf('/');
        return separatorIndex >= 0 ? normalized[(separatorIndex + 1)..] : normalized;
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

    private static int? ParseYear(string? value)
    {
        var date = ParseDate(value);
        return date?.Year;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
        {
            return date.ToUniversalTime();
        }

        return null;
    }

    private static float? ParseFloat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static string MapPersonKind(IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            var normalized = role.Trim().ToLowerInvariant();
            if (normalized.Contains("director", StringComparison.Ordinal))
            {
                return PersonType.Director;
            }

            if (normalized.Contains("writer", StringComparison.Ordinal) || normalized.Contains("screenplay", StringComparison.Ordinal))
            {
                return PersonType.Writer;
            }

            if (normalized.Contains("producer", StringComparison.Ordinal))
            {
                return PersonType.Producer;
            }

            if (normalized.Contains("composer", StringComparison.Ordinal) || normalized.Contains("music", StringComparison.Ordinal))
            {
                return PersonType.Composer;
            }
        }

        return string.Empty;
    }

    private static void AddTrailerMetaIfYouTube(ApiMediaDetail detail, ICollection<string> urls)
    {
        var trailer = GetMeta(detail, "trailer");
        if (string.IsNullOrWhiteSpace(trailer))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(trailer);
            if (document.RootElement.TryGetProperty("sourceUrl", out var sourceUrl))
            {
                AddIfYouTube(urls, sourceUrl.GetString());
            }
        }
        catch (JsonException)
        {
        }
    }

    private static void AddIfYouTube(ICollection<string> urls, string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        if (uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            urls.Add(url.Trim());
        }
    }

    private static Regex CodeRegex()
    {
        return new Regex("[A-Za-z]{2,10}[-_ ]?\\d{2,6}", RegexOptions.Compiled);
    }
}
