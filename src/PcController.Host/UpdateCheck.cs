using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PcController.Host;

// A release notice taken from a manifest signed with an offline release key (AUTO_UPDATE_PLAN.md).
public sealed record UpdateRelease(string Version, string ReleaseUrl, string Summary, bool Security, DateTimeOffset Published);
public enum UpdateOutcome { UpToDate, Available, Failed }
public sealed record UpdateResult(UpdateOutcome Outcome, UpdateRelease? Release = null, string? Problem = null);

// Phase 1 only checks and notifies: it reads the GitHub release list and a signed update.json,
// never downloads an installer or archive, and never runs anything. Every request is HTTPS to a
// fixed GitHub host, carries no identifier and is size- and time-limited.
public sealed partial class UpdateChecker : IDisposable
{
    public const string Repository = "maamouribilel/pc-remote";
    internal static readonly Uri Feed = new($"https://api.github.com/repos/{Repository}/releases?per_page=20");
    // GitHub redirects release downloads to its asset host (release-assets.* on 2026-10-15).
    internal static readonly string[] Hosts = ["api.github.com", "github.com", "release-assets.githubusercontent.com", "objects.githubusercontent.com"];
    internal const int ListLimit = 4 << 20, ManifestLimit = 64 << 10, SignatureLimit = 1024, SummaryLimit = 300, RedirectLimit = 5;
    private static readonly Dictionary<string, (string Platform, string Kind)> AssetKinds = new()
    {
        ["-win-x64-setup.exe"] = ("win-x64", "installer"), ["-win-x64.zip"] = ("win-x64", "zip"),
        ["-osx-arm64.zip"] = ("osx-arm64", "zip"), ["-osx-x64.zip"] = ("osx-x64", "zip"), ["-linux-x64.tar.gz"] = ("linux-x64", "archive"),
    };
    private static readonly JsonSerializerOptions Json = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true };
    private readonly HttpClient http;
    private readonly ECDsa[] keys;
    public static UpdateChecker CreateProduction() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(15),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
    }, LoadReleaseKeys());
    internal UpdateChecker(HttpMessageHandler handler, ECDsa[] keys)
    {
        if (keys.Length == 0) throw new ArgumentException("At least one release key is required.", nameof(keys));
        this.keys = keys; http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }
    public void Dispose() { http.Dispose(); foreach (var key in keys) key.Dispose(); }

    public async Task<UpdateResult> CheckAsync(Version current, CancellationToken token = default)
    {
        current = Normalize(current);
        List<GitHubRelease> releases;
        try { releases = JsonSerializer.Deserialize<List<GitHubRelease>>(await GetAsync(Feed, ListLimit, "application/vnd.github+json", token)) ?? []; }
        catch (UpdateCheckException e) { return new(UpdateOutcome.Failed, Problem: e.Message); }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException && !token.IsCancellationRequested)
        { return new(UpdateOutcome.Failed, Problem: e is JsonException ? "GitHub returned an unexpected release list." : "GitHub could not be reached."); }
        var candidates = releases.Where(r => !r.Draft).Select(r => (Release: r, Version: TryParseTag(r.Tag)))
            .Where(c => c.Version != null && c.Version > current).OrderByDescending(c => c.Version);
        string? problem = null;
        foreach (var (release, version) in candidates)
        {
            var manifestAsset = release.Assets?.FirstOrDefault(a => a.Name == "update.json");
            var signatureAsset = release.Assets?.FirstOrDefault(a => a.Name == "update.json.sig");
            // Releases published before update checks existed carry no manifest.
            if (manifestAsset == null || signatureAsset == null) continue;
            try
            {
                var manifest = await GetAsync(AssetUri(release.Tag!, manifestAsset, ManifestLimit), ManifestLimit, "application/octet-stream", token);
                var signature = await GetAsync(AssetUri(release.Tag!, signatureAsset, SignatureLimit), SignatureLimit, "application/octet-stream", token);
                if (!Verify(manifest, signature, keys)) throw new UpdateCheckException($"Release {release.Tag} is not signed with a trusted PC Remote release key.");
                return new(UpdateOutcome.Available, ParseManifest(manifest, version!));
            }
            catch (UpdateCheckException e) { problem ??= e.Message; }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !token.IsCancellationRequested) { problem ??= $"Release {release.Tag} could not be downloaded."; }
        }
        return problem == null ? new(UpdateOutcome.UpToDate) : new(UpdateOutcome.Failed, Problem: problem + " No update is offered.");
    }

    private static Uri AssetUri(string tag, GitHubAsset asset, int limit)
    {
        if (asset.Size is <= 0 || asset.Size > limit) throw new UpdateCheckException($"Release {tag} has an oversized {asset.Name}.");
        var expected = $"https://github.com/{Repository}/releases/download/{tag}/{asset.Name}";
        if (asset.Url != expected) throw new UpdateCheckException($"Release {tag} lists {asset.Name} at an unexpected address.");
        return new Uri(expected);
    }

    private async Task<byte[]> GetAsync(Uri uri, int limit, string accept, CancellationToken token)
    {
        for (var hop = 0; hop <= RedirectLimit; hop++)
        {
            if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || !Hosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase) || uri.UserInfo.Length > 0)
                throw new UpdateCheckException("GitHub redirected to an address outside its release hosts.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("PC-Remote-update-check");
            request.Headers.Accept.ParseAdd(accept);
            if (uri.IdnHost == "api.github.com") request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                uri = response.Headers.Location is { } location ? new Uri(uri, location) : throw new UpdateCheckException("GitHub sent a redirect without a location.");
                continue;
            }
            if (response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests) throw new UpdateCheckException("GitHub's request limit was reached. Try again later.");
            if (!response.IsSuccessStatusCode) throw new UpdateCheckException($"GitHub answered HTTP {(int)response.StatusCode}.");
            if (response.Content.Headers.ContentLength > limit) throw new UpdateCheckException("GitHub returned more data than an update check expects.");
            await using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            for (int read; (read = await stream.ReadAsync(chunk, token).ConfigureAwait(false)) > 0;)
            {
                if (buffer.Length + read > limit) throw new UpdateCheckException("GitHub returned more data than an update check expects.");
                buffer.Write(chunk, 0, read);
            }
            return buffer.ToArray();
        }
        throw new UpdateCheckException("GitHub redirected too many times.");
    }

    // update.json.sig: one line of Base64 holding the DER ECDSA P-256/SHA-256 signature of the exact update.json bytes.
    internal static bool Verify(byte[] manifest, byte[] signatureFile, IEnumerable<ECDsa> trusted)
    {
        byte[] signature;
        try { signature = Convert.FromBase64String(Encoding.ASCII.GetString(signatureFile).Trim()); }
        catch (FormatException) { return false; }
        return trusted.Any(key => key.VerifyData(manifest, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
    }

    internal static UpdateRelease ParseManifest(byte[] data, Version expected)
    {
        UpdateManifest manifest;
        try { manifest = JsonSerializer.Deserialize<UpdateManifest>(data, Json) ?? throw new UpdateCheckException("The signed manifest is empty."); }
        catch (JsonException) { throw new UpdateCheckException("The signed manifest is malformed."); }
        var version = $"{expected.Major}.{expected.Minor}.{expected.Build}";
        if (manifest.Schema != 1 || manifest.Product != "pc-remote") throw new UpdateCheckException("The signed manifest uses an unsupported format.");
        if (manifest.Version != version || manifest.Tag != "v" + version) throw new UpdateCheckException($"The signed manifest does not describe release v{version}.");
        if (manifest.ReleaseUrl != ReleasePage(version)) throw new UpdateCheckException("The signed manifest names an unexpected release page.");
        if (!ValidSummary(manifest.Summary)) throw new UpdateCheckException("The signed manifest summary is not plain text.");
        if (manifest.Assets.Length == 0 || manifest.Assets.Any(a => !ValidAsset(a, version))) throw new UpdateCheckException("The signed manifest lists an unexpected download.");
        return new(version, manifest.ReleaseUrl, manifest.Summary, manifest.Security, manifest.Published);
    }
    private static bool ValidAsset(UpdateAsset asset, string version) =>
        AssetKinds.Any(k => asset.Name == $"PCRemote-{version}{k.Key}" && asset.Platform == k.Value.Platform && asset.Kind == k.Value.Kind)
        && asset.Size is > 0 and <= 1L << 31 && Sha256().IsMatch(asset.Sha256);
    internal static bool ValidSummary(string summary) => summary.Length is > 0 and <= SummaryLimit && !summary.Any(char.IsControl);
    internal static string ReleasePage(string version) => $"https://github.com/{Repository}/releases/tag/v{version}";
    internal static bool ValidRelease(UpdateRelease release) =>
        TryParseVersion(release.Version) != null && release.ReleaseUrl == ReleasePage(release.Version) && ValidSummary(release.Summary);

    internal static Version? TryParseTag(string? tag) => tag is ['v', .. var rest] ? TryParseVersion(rest) : null;
    internal static Version? TryParseVersion(string? text)
    {
        var match = text == null ? null : VersionPattern().Match(text);
        return match is { Success: true } ? new Version(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value)) : null;
    }
    internal static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));

    internal static ECDsa[] LoadReleaseKeys()
    {
        using var stream = typeof(UpdateChecker).Assembly.GetManifestResourceStream("PcController.Host.update-signing-keys.pub")
            ?? throw new InvalidOperationException("The release update key is missing from this build.");
        using var reader = new StreamReader(stream, Encoding.ASCII);
        return ReadKeys(reader.ReadToEnd());
    }
    internal static ECDsa[] ReadKeys(string pem)
    {
        var keys = new List<ECDsa>();
        for (var rest = pem; PemEncoding.TryFind(rest, out var fields); rest = rest[fields.Location.End..])
        {
            if (rest[fields.Label] != "PUBLIC KEY") continue;
            var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(rest[fields.Base64Data]), out _);
            if (key.KeySize != 256) { key.Dispose(); throw new InvalidOperationException("Release update keys must be ECDSA P-256."); }
            keys.Add(key);
        }
        return keys.Count > 0 ? [.. keys] : throw new InvalidOperationException("No release update key is configured.");
    }

    [GeneratedRegex(@"^(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})$")] private static partial Regex VersionPattern();
    [GeneratedRegex("^[0-9a-f]{64}$")] private static partial Regex Sha256();

    private sealed record GitHubRelease([property: JsonPropertyName("tag_name")] string? Tag, [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);
    private sealed record GitHubAsset([property: JsonPropertyName("name")] string? Name, [property: JsonPropertyName("browser_download_url")] string? Url,
        [property: JsonPropertyName("size")] long Size);
    private sealed record UpdateManifest(int Schema, string Product, string Version, string Tag, string ReleaseUrl, DateTimeOffset Published,
        bool Security, string Summary, UpdateAsset[] Assets);
    private sealed record UpdateAsset(string Platform, string Kind, string Name, long Size, string Sha256);
}

public sealed class UpdateCheckException(string message) : Exception(message);
