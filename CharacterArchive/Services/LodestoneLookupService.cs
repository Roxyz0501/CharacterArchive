using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CharacterArchive.Services;

public sealed record LodestoneLookupResult(string LodestoneId, string? ProfileImageUrl);

public sealed partial class LodestoneLookupService : IDisposable
{
    private const int MaximumImageBytes = 5 * 1024 * 1024;
    private const int MaximumHtmlBytes = 2 * 1024 * 1024;
    private readonly HttpClient client;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim requestLock = new(1, 1);
    private long? lastRequest;

    public LodestoneLookupService() : this(new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(20) }, TimeProvider.System) { }

    internal LodestoneLookupService(HttpClient client, TimeProvider clock)
    {
        this.client = client;
        this.clock = clock;
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CharacterArchive-Dalamud/1.1");
    }

    public async Task<LodestoneLookupResult?> FindAsync(string characterName, string homeWorld, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        cancellationToken = deadline.Token;
        await requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var url = $"https://na.finalfantasyxiv.com/lodestone/character/?q={WebUtility.UrlEncode(characterName)}&worldname={WebUtility.UrlEncode(homeWorld)}";
            using var search = await GetAsync(new Uri(url), cancellationToken).ConfigureAwait(false);
            var html = Encoding.UTF8.GetString(await ReadBoundedAsync(search.Content, MaximumHtmlBytes, cancellationToken).ConfigureAwait(false));
            var id = FindExactCharacterId(html, characterName, homeWorld);
            if (id is null) return null;
            using var profile = await GetAsync(new Uri($"https://na.finalfantasyxiv.com/lodestone/character/{id}/"), cancellationToken).ConfigureAwait(false);
            var profileHtml = Encoding.UTF8.GetString(await ReadBoundedAsync(profile.Content, MaximumHtmlBytes, cancellationToken).ConfigureAwait(false));
            if (!MatchesCharacter(profileHtml, "frame__chara__name", "frame__chara__world", characterName, homeWorld)) return null;
            return new(id, ExtractProfileImageUrl(profileHtml));
        }
        finally { requestLock.Release(); }
    }

    internal static string? FindExactCharacterId(string html, string name, string world)
    {
        var ids = CharacterCardRegex().Matches(html)
            .Where(m => MatchesCharacter(m.Groups["body"].Value, "entry__name", "entry__world", name, world))
            .Select(m => m.Groups["id"].Value).Distinct().Take(2).ToArray();
        return ids.Length == 1 ? ids[0] : null;
    }

    private static bool MatchesCharacter(string html, string nameClass, string worldClass, string name, string world)
    {
        var actualName = ElementText(html, nameClass);
        var actualWorld = ElementText(html, worldClass).Split('[')[0].Trim();
        return string.Equals(actualName, Normalize(name), StringComparison.OrdinalIgnoreCase) &&
               string.Equals(actualWorld, Normalize(world), StringComparison.OrdinalIgnoreCase);
    }

    private static string ElementText(string html, string cssClass)
    {
        var match = Regex.Match(html, """<(?:p|span|div)\b[^>]*class=["'][^"']*\b""" +
            Regex.Escape(cssClass) + """\b[^"']*["'][^>]*>(.*?)</(?:p|span|div)>""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        return match.Success ? Normalize(WebUtility.HtmlDecode(TagsRegex().Replace(match.Groups[1].Value, " "))) : string.Empty;
    }

    private static string Normalize(string text) => WhitespaceRegex().Replace(text, " ").Trim();

    internal static bool IsOfficialUri(Uri uri) => uri.Scheme == Uri.UriSchemeHttps &&
        (uri.Host.Equals("finalfantasyxiv.com", StringComparison.OrdinalIgnoreCase) ||
         uri.Host.EndsWith(".finalfantasyxiv.com", StringComparison.OrdinalIgnoreCase)) &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.IsDefaultPort;

    // Called only under requestLock. Every request, including redirects and failed attempts, is spaced.
    private async Task<HttpResponseMessage> GetAsync(Uri uri, CancellationToken token)
    {
        for (var redirects = 0; redirects <= 3; redirects++)
        {
            if (!IsOfficialUri(uri)) throw new InvalidDataException("Lodestone redirected outside the official HTTPS hosts.");
            if (lastRequest is { } previous)
            {
                var remaining = TimeSpan.FromSeconds(1) - clock.GetElapsedTime(previous);
                if (remaining > TimeSpan.Zero) await Task.Delay(remaining, clock, token).ConfigureAwait(false);
            }
            lastRequest = clock.GetTimestamp();
            var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if (response.RequestMessage?.RequestUri is { } finalUri && !IsOfficialUri(finalUri))
            {
                response.Dispose();
                throw new InvalidDataException("Unexpected response host.");
            }
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location is null || redirects == 3) throw new InvalidDataException("Invalid Lodestone redirect.");
                uri = location.IsAbsoluteUri ? location : new Uri(uri, location);
                continue;
            }
            try { response.EnsureSuccessStatusCode(); return response; }
            catch { response.Dispose(); throw; }
        }
        throw new InvalidDataException("Too many redirects.");
    }

    private static async Task<byte[]> ReadBoundedAsync(HttpContent content, int limit, CancellationToken token)
    {
        if (content.Headers.ContentLength > limit) throw new InvalidDataException("Lodestone response is too large.");
        await using var source = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var block = new byte[16384];
        while (true)
        {
            var count = await source.ReadAsync(block, token).ConfigureAwait(false);
            if (count == 0) break;
            if (buffer.Length + count > limit) throw new InvalidDataException("Lodestone response is too large.");
            buffer.Write(block, 0, count);
        }
        return buffer.ToArray();
    }

    public async Task<string?> DownloadProfileImageAsync(string imageUrl, string destinationPath, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || !IsOfficialUri(uri)) return null;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        cancellationToken = deadline.Token;
        await requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var response = await GetAsync(uri, cancellationToken).ConfigureAwait(false);
            if (response.Content.Headers.ContentType?.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                return null;
            var bytes = await ReadBoundedAsync(response.Content, MaximumImageBytes, cancellationToken).ConfigureAwait(false);
            if (!IsImage(bytes)) return null;
            if (File.Exists(destinationPath) && new FileInfo(destinationPath).Length == bytes.Length &&
                (await File.ReadAllBytesAsync(destinationPath, cancellationToken).ConfigureAwait(false)).AsSpan().SequenceEqual(bytes))
                return destinationPath;
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temporary = destinationPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                await using (var destination = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                    destination.Flush(true);
                }
                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporary, destinationPath, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return destinationPath;
        }
        finally { requestLock.Release(); }
    }

    private static bool IsImage(byte[] bytes) =>
        bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
        (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) ||
        (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8));

    public void Dispose() { client.Dispose(); requestLock.Dispose(); }

    internal static string? ExtractProfileImageUrl(string html)
    {
        var match = CharacterFaceRegex().Match(html);
        // Generic OpenGraph images can be site logos or full-body portraits, not face icons.
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    [GeneratedRegex("""<a\b[^>]*href=["'][^"']*/lodestone/character/(?<id>\d+)/[^"']*["'][^>]*>(?<body>.*?)</a>""",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CharacterCardRegex();

    [GeneratedRegex("""class=["']frame__chara__face["'][^>]*>[\s\S]{0,1000}?<img[^>]+src=["']([^"']+)["']""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CharacterFaceRegex();

    [GeneratedRegex("<[^>]*>", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex TagsRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex WhitespaceRegex();
}
