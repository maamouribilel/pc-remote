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

// Checks read the GitHub release list and a signed update.json and never run anything. Only an
// explicit download (Phase 2) fetches a release file: the one the freshly re-verified manifest names
// for this platform, checked against its signed size and SHA-256. Every request is HTTPS to a fixed
// GitHub host, carries no identifier and is size- and time-limited.
public sealed partial class UpdateChecker : IDisposable
{
    public const string Repository = "maamouribilel/pc-remote";
    internal static readonly Uri Feed = new($"https://api.github.com/repos/{Repository}/releases?per_page=20");
    // GitHub redirects release downloads to its asset host (release-assets.* on 2026-10-15).
    internal static readonly string[] Hosts = ["api.github.com", "github.com", "release-assets.githubusercontent.com", "objects.githubusercontent.com"];
    internal const int ListLimit = 4 << 20, ManifestLimit = 64 << 10, SignatureLimit = 1024, SummaryLimit = 300, RedirectLimit = 5;
    private const string Octet = "application/octet-stream";
    private const UnixFileMode UserOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    private static readonly Dictionary<string, (string Platform, string Kind)> AssetKinds = new()
    {
        ["-win-x64-setup.exe"] = ("win-x64", "installer"), ["-win-x64.zip"] = ("win-x64", "zip"),
        ["-osx-arm64.zip"] = ("osx-arm64", "zip"), ["-osx-x64.zip"] = ("osx-x64", "zip"), ["-linux-x64.tar.gz"] = ("linux-x64", "archive"),
    };
    private static readonly JsonSerializerOptions Json = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true };
    private readonly HttpClient http;
    private readonly ECDsa[] keys;
    // HttpClient.Timeout ends at the response headers; these also bound a body that stalls.
    private readonly TimeSpan requestTimeout, stallTimeout;
    public static UpdateChecker CreateProduction() => new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(15),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
    }, LoadReleaseKeys());
    internal UpdateChecker(HttpMessageHandler handler, ECDsa[] keys, TimeSpan? requestTimeout = null, TimeSpan? stallTimeout = null)
    {
        if (keys.Length == 0) throw new ArgumentException("At least one release key is required.", nameof(keys));
        this.keys = keys; this.requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(30); this.stallTimeout = stallTimeout ?? TimeSpan.FromSeconds(60);
        http = new HttpClient(handler) { Timeout = this.requestTimeout };
    }
    public void Dispose() { http.Dispose(); foreach (var key in keys) key.Dispose(); }

    public async Task<UpdateResult> CheckAsync(Version current, CancellationToken token = default)
    {
        current = Normalize(current);
        List<GitHubRelease> releases;
        try { releases = JsonSerializer.Deserialize<List<GitHubRelease>>(await GetAsync(Feed, ListLimit, "application/vnd.github+json", token)) ?? []; }
        catch (UpdateCheckException e) { return new(UpdateOutcome.Failed, Problem: e.Message); }
        catch (Exception e) when (e is HttpRequestException or JsonException or OperationCanceledException && !token.IsCancellationRequested)
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
                var manifest = await GetAsync(AssetUri(release.Tag!, manifestAsset, ManifestLimit), ManifestLimit, Octet, token);
                var signature = await GetAsync(AssetUri(release.Tag!, signatureAsset, SignatureLimit), SignatureLimit, Octet, token);
                if (!Verify(manifest, signature, keys)) throw new UpdateCheckException($"Release {release.Tag} is not signed with a trusted PC Remote release key.");
                return new(UpdateOutcome.Available, ParseManifest(manifest, version!));
            }
            catch (UpdateCheckException e) { problem ??= e.Message; }
            catch (Exception e) when (e is HttpRequestException or OperationCanceledException && !token.IsCancellationRequested) { problem ??= $"Release {release.Tag} could not be downloaded."; }
        }
        return problem == null ? new(UpdateOutcome.UpToDate) : new(UpdateOutcome.Failed, Problem: problem + " No update is offered.");
    }

    private static Uri AssetUri(string tag, GitHubAsset asset, int limit)
    {
        if (asset.Size is <= 0 || asset.Size > limit) throw new UpdateCheckException($"Release {tag} has an oversized {asset.Name}.");
        var expected = ReleaseDownload(tag, asset.Name ?? "");
        if (asset.Url != expected.AbsoluteUri) throw new UpdateCheckException($"Release {tag} lists {asset.Name} at an unexpected address.");
        return expected;
    }
    private static Uri ReleaseDownload(string tag, string name) => new($"https://github.com/{Repository}/releases/download/{tag}/{name}");

    // Phase 2, only on request: re-reads and re-verifies the release's signed manifest, then downloads the file it
    // names for this platform. Size and SHA-256 are checked while streaming and again through a read-only handle that
    // stays open until the caller disposes it: on Windows nobody can change, rename or delete the file meanwhile.
    public async Task<VerifiedDownload> DownloadAsync(string version, string platform, string kind, string directory, IProgress<double>? progress = null, CancellationToken token = default)
    {
        var expected = TryParseVersion(version) ?? throw new UpdateCheckException("The offered version is not valid.");
        var tag = "v" + version;
        var manifest = await GetAsync(ReleaseDownload(tag, "update.json"), ManifestLimit, Octet, token).ConfigureAwait(false);
        var signature = await GetAsync(ReleaseDownload(tag, "update.json.sig"), SignatureLimit, Octet, token).ConfigureAwait(false);
        if (!Verify(manifest, signature, keys)) throw new UpdateCheckException($"Release {tag} is not signed with a trusted PC Remote release key.");
        var asset = ReadManifest(manifest, expected).Assets.SingleOrDefault(a => a.Platform == platform && a.Kind == kind)
            ?? throw new UpdateCheckException($"Release {tag} has no {kind} for this computer.");
        if (OperatingSystem.IsWindows()) Directory.CreateDirectory(directory); else Directory.CreateDirectory(directory, UserOnly);
        var path = Path.Combine(directory, asset.Name); var partial = path + ".partial";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            await using (var output = new FileStream(partial, options))
                await DownloadToAsync(ReleaseDownload(tag, asset.Name), asset, output, progress, token).ConfigureAwait(false);
            File.Move(partial, path, true);
        }
        catch { TryDelete(partial); throw; }
        // Verify what is held: reopen read-only with read-only sharing and hash through that same handle.
        var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        try
        {
            if (locked.Length != asset.Size || Convert.ToHexStringLower(await SHA256.HashDataAsync(locked, token).ConfigureAwait(false)) != asset.Sha256)
                throw new UpdateCheckException("The downloaded file changed after it was checked.");
            locked.Position = 0;
            return new(version, path, locked);
        }
        catch { await locked.DisposeAsync().ConfigureAwait(false); TryDelete(path); throw; }
    }
    private async Task DownloadToAsync(Uri uri, UpdateAsset asset, Stream output, IProgress<double>? progress, CancellationToken token)
    {
        // A large download may take minutes; it fails only when no data arrives for stallTimeout.
        using var stall = CancellationTokenSource.CreateLinkedTokenSource(token);
        stall.CancelAfter(requestTimeout);
        using var response = await OpenAsync(uri, Octet, stall.Token).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength is { } length && length != asset.Size) throw new UpdateCheckException("GitHub returned a download of the wrong size.");
        await using var stream = await response.Content.ReadAsStreamAsync(stall.Token).ConfigureAwait(false);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var chunk = new byte[80 * 1024]; long total = 0;
        stall.CancelAfter(stallTimeout);
        for (int read; (read = await stream.ReadAsync(chunk, stall.Token).ConfigureAwait(false)) > 0;)
        {
            total += read;
            if (total > asset.Size) throw new UpdateCheckException("GitHub returned more data than the signed download size.");
            hash.AppendData(chunk, 0, read);
            await output.WriteAsync(chunk.AsMemory(0, read), stall.Token).ConfigureAwait(false);
            stall.CancelAfter(stallTimeout); progress?.Report((double)total / asset.Size);
        }
        if (total != asset.Size) throw new UpdateCheckException("The download ended before its signed size.");
        if (Convert.ToHexStringLower(hash.GetHashAndReset()) != asset.Sha256) throw new UpdateCheckException("The download does not match its signed SHA-256.");
        await output.FlushAsync(token).ConfigureAwait(false);
    }
    // Names this feature writes into a download folder: release files and their .partial downloads.
    internal static bool IsDownloadName(string name) => DownloadName().IsMatch(name) && AssetKinds.Keys.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal) || name.EndsWith(suffix + ".partial", StringComparison.Ordinal));
    private static void TryDelete(string path) { try { File.Delete(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { } }

    private async Task<byte[]> GetAsync(Uri uri, int limit, string accept, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(requestTimeout);
        using var response = await OpenAsync(uri, accept, timeout.Token).ConfigureAwait(false);
        if (response.Content.Headers.ContentLength > limit) throw new UpdateCheckException("GitHub returned more data than an update check expects.");
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        for (int read; (read = await stream.ReadAsync(chunk, timeout.Token).ConfigureAwait(false)) > 0;)
        {
            if (buffer.Length + read > limit) throw new UpdateCheckException("GitHub returned more data than an update check expects.");
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    // Follows at most RedirectLimit redirects, each to an HTTPS GitHub release host. The caller disposes the response.
    private async Task<HttpResponseMessage> OpenAsync(Uri uri, string accept, CancellationToken token)
    {
        for (var hop = 0; hop <= RedirectLimit; hop++)
        {
            if (uri.Scheme != Uri.UriSchemeHttps || !uri.IsDefaultPort || !Hosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase) || uri.UserInfo.Length > 0)
                throw new UpdateCheckException("GitHub redirected to an address outside its release hosts.");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd("PC-Remote-update-check");
            request.Headers.Accept.ParseAdd(accept);
            if (uri.IdnHost == "api.github.com") request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
            if ((int)response.StatusCode is 301 or 302 or 303 or 307 or 308)
            {
                using (response) uri = response.Headers.Location is { } location ? new Uri(uri, location) : throw new UpdateCheckException("GitHub sent a redirect without a location.");
                continue;
            }
            if (response.IsSuccessStatusCode) return response;
            var status = response.StatusCode; response.Dispose();
            if (status is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests) throw new UpdateCheckException("GitHub's request limit was reached. Try again later.");
            throw new UpdateCheckException($"GitHub answered HTTP {(int)status}.");
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

    internal static UpdateRelease ParseManifest(byte[] data, Version expected) => ReadManifest(data, expected).Release;
    private static (UpdateRelease Release, UpdateAsset[] Assets) ReadManifest(byte[] data, Version expected)
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
        return (new(version, manifest.ReleaseUrl, manifest.Summary, manifest.Security, manifest.Published), manifest.Assets);
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
    [GeneratedRegex(@"^PCRemote-\d{1,5}\.\d{1,5}\.\d{1,5}-[a-z0-9-]+\.(exe|zip|tar\.gz)(\.partial)?$")] private static partial Regex DownloadName();

    private sealed record GitHubRelease([property: JsonPropertyName("tag_name")] string? Tag, [property: JsonPropertyName("draft")] bool Draft,
        [property: JsonPropertyName("assets")] GitHubAsset[]? Assets);
    private sealed record GitHubAsset([property: JsonPropertyName("name")] string? Name, [property: JsonPropertyName("browser_download_url")] string? Url,
        [property: JsonPropertyName("size")] long Size);
    private sealed record UpdateManifest(int Schema, string Product, string Version, string Tag, string ReleaseUrl, DateTimeOffset Published,
        bool Security, string Summary, UpdateAsset[] Assets);
    private sealed record UpdateAsset(string Platform, string Kind, string Name, long Size, string Sha256);
}

public sealed class UpdateCheckException(string message) : Exception(message);

// A release file whose signed size and SHA-256 were verified through the read-only handle held here.
// On Windows the handle's read-only sharing stops other processes from changing, renaming or deleting
// the file until Dispose; it does not stop them from reading or running it.
public sealed class VerifiedDownload : IDisposable
{
    private readonly FileStream locked;
    public string Version { get; }
    public string Path { get; }
    internal VerifiedDownload(string version, string path, FileStream locked) { Version = version; Path = path; this.locked = locked; }
    public void Dispose() => locked.Dispose();
}
