using System.Net;
using System.Text.RegularExpressions;

namespace CharacterArchive.Services;

public sealed record LodestoneLookupResult(string LodestoneId, string? ProfileImageUrl);

public sealed partial class LodestoneLookupService : IDisposable
{
    private const int MaximumImageBytes = 5 * 1024 * 1024;
    private readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(20),
    };
    private readonly SemaphoreSlim requestLock = new(1, 1);

    public LodestoneLookupService()
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CharacterArchive-Dalamud/0.2");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
    }

    public async Task<LodestoneLookupResult?> FindAsync(
        string characterName,
        string homeWorld,
        CancellationToken cancellationToken)
    {
        await requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var query = WebUtility.UrlEncode(characterName);
            var world = WebUtility.UrlEncode(homeWorld);
            var searchUrl = $"https://na.finalfantasyxiv.com/lodestone/character/?q={query}&worldname={world}";
            var searchHtml = await client.GetStringAsync(searchUrl, cancellationToken).ConfigureAwait(false);
            var idMatch = CharacterIdRegex().Match(searchHtml);
            if (!idMatch.Success)
                return null;

            var id = idMatch.Groups[1].Value;
            var profileUrl = $"https://na.finalfantasyxiv.com/lodestone/character/{id}/";
            var profileHtml = await client.GetStringAsync(profileUrl, cancellationToken).ConfigureAwait(false);
            var imageUrl = ExtractProfileImageUrl(profileHtml);
            return new LodestoneLookupResult(id, imageUrl);
        }
        finally
        {
            requestLock.Release();
        }
    }

    public async Task<string?> DownloadProfileImageAsync(
        string imageUrl,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("finalfantasyxiv.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.EndsWith(".finalfantasyxiv.com", StringComparison.OrdinalIgnoreCase)))
            return null;

        await requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                return null;
            if (response.Content.Headers.ContentLength > MaximumImageBytes)
                return null;

            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var buffer = new byte[81920];
            var total = 0;
            while (true)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                total += read;
                if (total > MaximumImageBytes)
                {
                    destination.Close();
                    File.Delete(destinationPath);
                    return null;
                }
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }

            return destinationPath;
        }
        finally
        {
            requestLock.Release();
        }
    }

    public void Dispose()
    {
        client.Dispose();
        requestLock.Dispose();
    }

    internal static string? ExtractProfileImageUrl(string html)
    {
        var match = CharacterFaceRegex().Match(html);
        if (match.Success)
            return WebUtility.HtmlDecode(match.Groups[1].Value);

        match = OpenGraphImageRegex().Match(html);
        if (!match.Success)
            match = OpenGraphImageReverseRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    [GeneratedRegex("/lodestone/character/(\\d+)/", RegexOptions.CultureInvariant)]
    private static partial Regex CharacterIdRegex();

    [GeneratedRegex("class=[\"']frame__chara__face[\"'][^>]*>[\\s\\S]{0,500}?<img[^>]+src=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CharacterFaceRegex();

    [GeneratedRegex("property=[\"']og:image[\"'][^>]*content=[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OpenGraphImageRegex();

    [GeneratedRegex("content=[\"']([^\"']+)[\"'][^>]*property=[\"']og:image[\"']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OpenGraphImageReverseRegex();
}
