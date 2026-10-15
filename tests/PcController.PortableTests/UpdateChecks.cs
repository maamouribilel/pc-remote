using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PcController.Host;

// Update checks run against an in-memory GitHub; no test touches the network or the real release key.
internal static class UpdateChecks
{
    private const string Repo = "https://github.com/maamouribilel/pc-remote/releases";
    // Signed by scripts/update-manifest.py (openssl) with a throwaway key on 2026-10-15; the private key was deleted.
    private const string ScriptManifest = "ewogICJzY2hlbWEiOiAxLAogICJwcm9kdWN0IjogInBjLXJlbW90ZSIsCiAgInZlcnNpb24iOiAiOS44LjciLAogICJ0YWciOiAidjkuOC43IiwKICAicmVsZWFzZVVybCI6ICJodHRwczovL2dpdGh1Yi5jb20vbWFhbW91cmliaWxlbC9wYy1yZW1vdGUvcmVsZWFzZXMvdGFnL3Y5LjguNyIsCiAgInB1Ymxpc2hlZCI6ICIyMDI2LTEwLTE1VDAwOjAwOjAwWiIsCiAgInNlY3VyaXR5IjogZmFsc2UsCiAgInN1bW1hcnkiOiAiQ3Jvc3MtY2hlY2sgZml4dHVyZSBzaWduZWQgYnkgc2NyaXB0cy91cGRhdGUtbWFuaWZlc3QucHkuIiwKICAiYXNzZXRzIjogWwogICAgewogICAgICAicGxhdGZvcm0iOiAid2luLXg2NCIsCiAgICAgICJraW5kIjogImluc3RhbGxlciIsCiAgICAgICJuYW1lIjogIlBDUmVtb3RlLTkuOC43LXdpbi14NjQtc2V0dXAuZXhlIiwKICAgICAgInNpemUiOiA0MCwKICAgICAgInNoYTI1NiI6ICJlMmI2ODQzMGI3NWRkOTA0ZjdkZDFmNzFiMmRhYjRlY2E2Y2I1ZjVlZTFhYTYzNmZkYzJlNGI5NjUwNTNiZGY3IgogICAgfSwKICAgIHsKICAgICAgInBsYXRmb3JtIjogIm9zeC1hcm02NCIsCiAgICAgICJraW5kIjogInppcCIsCiAgICAgICJuYW1lIjogIlBDUmVtb3RlLTkuOC43LW9zeC1hcm02NC56aXAiLAogICAgICAic2l6ZSI6IDM2LAogICAgICAic2hhMjU2IjogImI4ODRjMGM5MWVmNjQ3OWQ1ZGVkOGExNGY3OThjMTMxYTU5YzNhZWEzZTUxOTExNTQxMzUyNmY5MzY1MjFmY2YiCiAgICB9LAogICAgewogICAgICAicGxhdGZvcm0iOiAibGludXgteDY0IiwKICAgICAgImtpbmQiOiAiYXJjaGl2ZSIsCiAgICAgICJuYW1lIjogIlBDUmVtb3RlLTkuOC43LWxpbnV4LXg2NC50YXIuZ3oiLAogICAgICAic2l6ZSI6IDM5LAogICAgICAic2hhMjU2IjogImViY2RlZGIyMjE3MDQ0MGJlYWVlYTVlOTI3NDk2OTQ1NWZmNjhkZmY4MzBmYjQwOTNmZjczMTYyZTcxYTQ2MjgiCiAgICB9CiAgXQp9Cg==";
    private const string ScriptSignature = "MEUCIE2TZgcdDKDfcRkcK8WWu+M354yiG8NMZzA6ocx+SqEeAiEAoV7hEp+WMplImAN0x4Dk/6G6AR7+/z20Mg9unDc0KUg=";
    private const string ScriptKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEgSphklAi7pFbGfhL0XRw11Ip1U5U8kpE79fk6nUrEF+tE3DSHZeCb6pSengyxFKpHviqb5Vsk+GNOCtacJatXQ==";

    internal static async Task Run(Action<bool, string> check)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var current = new Version(0, 4, 2);

        var production = UpdateChecker.LoadReleaseKeys();
        check(production.Length >= 1 && production.All(k => k.KeySize == 256), "the build embeds the committed ECDSA P-256 release update key");
        check(UpdateChecker.Feed.AbsoluteUri == "https://api.github.com/repos/maamouribilel/pc-remote/releases?per_page=20", "update checks read the fixed GitHub release list (pre-releases included)");
        using (var fixtureKey = ECDsa.Create())
        {
            fixtureKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(ScriptKey), out _);
            var manifest = Convert.FromBase64String(ScriptManifest); var signature = Encoding.ASCII.GetBytes(ScriptSignature + "\n");
            check(UpdateChecker.Verify(manifest, signature, [fixtureKey]) && UpdateChecker.ParseManifest(manifest, new Version(9, 8, 7)).Summary.StartsWith("Cross-check"),
                "a manifest signed by scripts/update-manifest.py verifies and parses in the companion");
            check(!UpdateChecker.Verify(manifest, signature, production), "the script fixture is rejected by the production release key");
            manifest[^3] ^= 1;
            check(!UpdateChecker.Verify(manifest, signature, [fixtureKey]), "one changed manifest byte fails verification");
        }
        check(UpdateChecker.ReadKeys("comment\n" + key.ExportSubjectPublicKeyInfoPem() + "\nnote\n" + other.ExportSubjectPublicKeyInfoPem()).Length == 2, "several trusted keys can be listed for rotation");
        using (var rsa = RSA.Create(2048))
        {
            var refused = false;
            try { UpdateChecker.ReadKeys(rsa.ExportSubjectPublicKeyInfoPem()); } catch (CryptographicException) { refused = true; } catch (InvalidOperationException) { refused = true; }
            check(refused, "a non-ECDSA key is never trusted");
        }

        // A verified newer release: browser_download_url redirects to GitHub's asset host, as observed on 2026-10-15.
        var github = new FakeGitHub();
        github.Release("v0.4.3", Manifest("0.4.3"), key);
        github.Release("v0.4.2", null, key); github.Release("v0.4.1", null, key);
        var result = await Check(github, key, current);
        check(result is { Outcome: UpdateOutcome.Available, Release: { Version: "0.4.3", Security: false, ReleaseUrl: Repo + "/tag/v0.4.3" } }, "a signed newer release is offered");
        check(github.Requests.All(r => r.Uri.Scheme == "https" && UpdateChecker.Hosts.Contains(r.Uri.Host)) && github.Requests.Any(r => r.Uri.Host == "release-assets.githubusercontent.com"),
            "requests stay on HTTPS GitHub hosts and follow the asset redirect");
        check(github.Requests.All(r => r.UserAgent == "PC-Remote-update-check" && r.Headers.All(h => h is "User-Agent" or "Accept" or "X-GitHub-Api-Version")),
            "requests carry no version, installation or device identifier");
        check(!github.Requests.Any(r => r.Uri.AbsolutePath.EndsWith(".exe") || r.Uri.AbsolutePath.EndsWith(".zip") || r.Uri.AbsolutePath.EndsWith(".tar.gz")), "a check never downloads an installer or archive");

        github = new FakeGitHub(); github.Release("v0.4.2", Manifest("0.4.2"), key); github.Release("v0.4.1", Manifest("0.4.1"), key);
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.UpToDate && github.Requests.Count == 1, "the current or older releases are never offered or downloaded");

        github = new FakeGitHub(); github.Release("v0.4.9", Manifest("0.4.9"), key, draft: true); github.Release("v0.5.0-beta", Manifest("0.5.0"), key); github.Release("0.6.0", Manifest("0.6.0"), key);
        github.Release("v0.4.3", null, key);
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.UpToDate, "drafts, non-vX.Y.Z tags and releases without update.json are skipped");

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), other);
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Problem!.Contains("not signed with a trusted"), "a manifest signed by another key is refused");

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key); github.Tamper("v0.4.3", Manifest("0.4.3", summary: "Install this other build."));
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Release == null, "a manifest changed after signing is refused");

        foreach (var (name, manifest) in new[]
        {
            ("a manifest whose version differs from its release tag", Manifest("0.4.4", tag: "v0.4.4")),
            ("a manifest naming a foreign release page", Manifest("0.4.3", url: "https://example.com/pc-remote")),
            ("a manifest with control characters in its summary", Manifest("0.4.3", summary: "Fix\u001b[31m")),
            ("a manifest listing an unexpected download", Manifest("0.4.3", asset: "PCRemote-0.4.3-win-x64-setup.msi")),
            ("a manifest with an unknown schema", Manifest("0.4.3", schema: 2)),
        })
        {
            github = new FakeGitHub(); github.Release("v0.4.3", manifest, key);
            result = await Check(github, key, current);
            check(result.Outcome == UpdateOutcome.Failed && result.Release == null, "a correctly signed but invalid manifest is refused: " + name);
        }

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key); github.RedirectTo = "https://release-assets.example.net/x";
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Problem!.Contains("outside"), "a redirect to a non-GitHub host is refused");

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key); github.RedirectTo = "http://release-assets.githubusercontent.com/x";
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed, "a redirect to plain HTTP is refused");

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key, url: "https://github.com/someone-else/pc-remote/releases/download/v0.4.3/update.json");
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Problem!.Contains("unexpected address"), "an asset outside this repository's release downloads is refused");

        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key, listedSize: UpdateChecker.ManifestLimit + 1);
        check((await Check(github, key, current)).Outcome == UpdateOutcome.Failed, "an oversized manifest listing is refused before download");
        github = new FakeGitHub(); github.Release("v0.4.3", Manifest("0.4.3"), key); github.Oversize = true;
        check((await Check(github, key, current)).Outcome == UpdateOutcome.Failed, "a response that grows past the limit is cut off");

        github = new FakeGitHub(); github.Release("v0.4.5", Manifest("0.4.5"), other); github.Release("v0.4.3", Manifest("0.4.3"), key);
        result = await Check(github, key, current);
        check(result is { Outcome: UpdateOutcome.Available, Release.Version: "0.4.3" }, "an unverifiable higher release does not hide a verified newer one");

        github = new FakeGitHub { ListStatus = HttpStatusCode.Forbidden };
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Problem!.Contains("request limit"), "GitHub rate limiting is reported, not crashed on");
        github = new FakeGitHub { Offline = true };
        result = await Check(github, key, current);
        check(result.Outcome == UpdateOutcome.Failed && result.Problem!.Contains("could not be reached"), "an offline computer reports a failed check");
        github = new FakeGitHub { ListBody = "{\"not\":\"a list\"}" };
        check((await Check(github, key, current)).Outcome == UpdateOutcome.Failed, "an unexpected release list is reported");

        await Service(check);
    }

    private static async Task Service(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "PcUpdateTests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var available = new UpdateRelease("0.4.3", Repo + "/tag/v0.4.3", "Fixes a fixture.", false, DateTimeOffset.UtcNow);
            var calls = 0; var next = new UpdateResult(UpdateOutcome.Available, available);
            Task<UpdateResult> Fake(Version version, CancellationToken token) { calls++; return Task.FromResult(next); }
            using (var service = new UpdateService(directory, new Version(0, 4, 2, 0), Fake))
            {
                var changed = 0; service.Changed += () => changed++;
                service.Start();
                check(service.NeedsConsent && service.Notice == null && calls == 0 && service.Current == new Version(0, 4, 2), "nothing is checked before the one-time question is answered");
                await service.CheckNowAsync();
                check(calls == 1 && service.Notice == available && changed >= 2 && service.NeedsConsent, "Check now is a single explicit check and leaves the question open");
                service.SetAutomatic(false);
                check(!service.NeedsConsent && service.Preferences.Automatic == false && service.Status.Contains("0.4.3 is available"), "the answer is kept and the status names the available version");
                if (!OperatingSystem.IsWindows())
                    check(File.GetUnixFileMode(directory) == (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute)
                        && File.GetUnixFileMode(Path.Combine(directory, "update-settings.json")) == (UnixFileMode.UserRead | UnixFileMode.UserWrite), "update preferences are private to this user (0700/0600)");
            }
            using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
            {
                check(!service.NeedsConsent && service.Notice?.Version == "0.4.3", "the answer and last verified notice survive a restart");
                service.Later();
                check(service.Notice == null && service.Preferences.Skipped == null, "Later hides the notice for this session only");
            }
            using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
            {
                check(service.Notice?.Version == "0.4.3", "Later does not persist");
                service.Skip();
                check(service.Notice == null && service.Preferences.Skipped == "0.4.3", "Skip this version hides that version");
                next = new UpdateResult(UpdateOutcome.Failed, Problem: "GitHub could not be reached.");
                await service.CheckNowAsync();
                check(service.Problem != null && service.Status.StartsWith("Update check failed") && service.Preferences.Available == available, "a failed check keeps the last verified release");
            }
            using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
                check(service.Notice == null && service.Preferences.Skipped == "0.4.3", "a skipped version stays skipped after restart");
            using (var service = new UpdateService(directory, new Version(0, 4, 3), Fake))
            {
                check(service.Notice == null, "an installed release that reached the notice version hides it");
                next = new UpdateResult(UpdateOutcome.UpToDate);
                await service.CheckNowAsync();
                check(service.Preferences.Available == null && service.Status.Contains("0.4.3 is the newest"), "an up-to-date check clears the old notice");
            }
            var file = Path.Combine(directory, "update-settings.json");
            File.WriteAllText(file, JsonSerializer.Serialize(new UpdatePreferences(true, null, "nonsense", new UpdateRelease("9.9.9", "https://example.com/", "x", true, DateTimeOffset.UtcNow))));
            using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
                check(service.Preferences.Available == null && service.Preferences.Skipped == null && service.Preferences.Automatic == true, "an edited settings file cannot point the notice at another site");
            File.WriteAllText(file, "{ not json");
            using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
                check(service.NeedsConsent && service.Notice == null, "a damaged settings file only resets update preferences");
            if (!OperatingSystem.IsWindows())
            {
                var target = Path.Combine(directory, "elsewhere.json");
                File.WriteAllText(target, JsonSerializer.Serialize(new UpdatePreferences(true))); File.Delete(file); File.CreateSymbolicLink(file, target);
                using (var service = new UpdateService(directory, new Version(0, 4, 2), Fake))
                {
                    check(service.NeedsConsent, "a symlinked settings file is not followed");
                    service.SetAutomatic(false);
                }
                check((File.GetAttributes(file) & FileAttributes.ReparsePoint) == 0 && JsonSerializer.Deserialize<UpdatePreferences>(File.ReadAllText(target))!.Automatic == true
                    && File.ReadAllText(file).Contains("\"Automatic\":false"), "saving replaces a symlink instead of writing through it");
            }
            var started = new TaskCompletionSource();
            var stopping = new UpdateService(Path.Combine(directory, "stop"), new Version(0, 4, 2), async (_, token) => { started.SetResult(); await Task.Delay(Timeout.Infinite, token); return new(UpdateOutcome.UpToDate); });
            var running = stopping.CheckNowAsync(); await started.Task;
            check(stopping.Checking && !stopping.Status.Contains("failed"), "a running check is shown as checking");
            stopping.Dispose();
            check(await Task.WhenAny(running, Task.Delay(5000)) == running && running.IsCompletedSuccessfully, "exit cancels an in-flight check without an error");
        }
        finally { Directory.Delete(directory, true); }
    }

    private static async Task<UpdateResult> Check(FakeGitHub github, ECDsa key, Version current)
    {
        using var checker = new UpdateChecker(github, [ImportPublic(key)]);
        return await checker.CheckAsync(current);
    }
    private static ECDsa ImportPublic(ECDsa key) { var copy = ECDsa.Create(); copy.ImportSubjectPublicKeyInfo(key.ExportSubjectPublicKeyInfo(), out _); return copy; }

    private static byte[] Manifest(string version, string? tag = null, string? url = null, string summary = "Fixes a fixture.", string? asset = null, int schema = 1) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schema, product = "pc-remote", version, tag = tag ?? "v" + version, releaseUrl = url ?? $"{Repo}/tag/v{version}", published = "2026-10-15T00:00:00Z",
            security = false, summary, assets = new[] { new { platform = "win-x64", kind = "installer", name = asset ?? $"PCRemote-{version}-win-x64-setup.exe", size = 1234, sha256 = new string('a', 64) } },
        });

    private sealed class FakeGitHub : HttpMessageHandler
    {
        internal sealed record Request(Uri Uri, string UserAgent, string[] Headers);
        private readonly List<object> releases = [];
        private readonly Dictionary<string, byte[]> assets = [];
        internal readonly List<Request> Requests = [];
        internal HttpStatusCode ListStatus = HttpStatusCode.OK;
        internal string? ListBody, RedirectTo;
        internal bool Offline, Oversize;
        internal void Release(string tag, byte[]? manifest, ECDsa key, bool draft = false, string? url = null, int? listedSize = null)
        {
            var list = new List<object>();
            if (manifest != null)
            {
                var signature = Encoding.ASCII.GetBytes(Convert.ToBase64String(key.SignData(manifest, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)) + "\n");
                assets[tag + "/update.json"] = manifest; assets[tag + "/update.json.sig"] = signature;
                list.Add(new { name = "update.json", browser_download_url = url ?? $"{Repo}/download/{tag}/update.json", size = listedSize ?? manifest.Length });
                list.Add(new { name = "update.json.sig", browser_download_url = $"{Repo}/download/{tag}/update.json.sig", size = signature.Length });
            }
            list.Add(new { name = $"PCRemote-{tag[1..]}-win-x64-setup.exe", browser_download_url = $"{Repo}/download/{tag}/PCRemote-{tag[1..]}-win-x64-setup.exe", size = 1234 });
            releases.Add(new { tag_name = tag, draft, prerelease = true, html_url = $"{Repo}/tag/{tag}", assets = list });
        }
        internal void Tamper(string tag, byte[] manifest) => assets[tag + "/update.json"] = manifest;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var uri = request.RequestUri!;
            Requests.Add(new(uri, request.Headers.UserAgent.ToString(), request.Headers.Select(h => h.Key).ToArray()));
            if (Offline) throw new HttpRequestException("offline fixture");
            if (uri == UpdateChecker.Feed)
                return Task.FromResult(new HttpResponseMessage(ListStatus) { Content = new StringContent(ListBody ?? JsonSerializer.Serialize(releases)) });
            var prefix = Repo + "/download/";
            if (uri.AbsoluteUri.StartsWith(prefix))
            {
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new Uri(RedirectTo ?? "https://release-assets.githubusercontent.com/github-production-release-asset/" + Uri.EscapeDataString(uri.AbsoluteUri[prefix.Length..]));
                return Task.FromResult(response);
            }
            if (uri.Host == "release-assets.githubusercontent.com" && assets.TryGetValue(Uri.UnescapeDataString(uri.AbsolutePath["/github-production-release-asset/".Length..]), out var body))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = Oversize ? new StreamContent(new Unseekable(new byte[UpdateChecker.ManifestLimit + 10])) : new ByteArrayContent(body) });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
    // No Content-Length: the size limit must hold while streaming.
    private sealed class Unseekable(byte[] data) : MemoryStream(data) { public override bool CanSeek => false; }
}
