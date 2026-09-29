using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Mahjong.Plugin.Dalamud.Telemetry;

public sealed class EndpointResolver
{
    // This fork ships no telemetry. Upstream resolves an upload endpoint from a URL
    // it controls and defaults to enabled, which would mean this fork's users sending
    // game logs, error dumps, memory dumps, input traces and signature probes to a
    // third party that can repoint the destination at any time without a rebuild.
    // The constants stay so the surrounding types and tests keep their shape; nothing
    // reads them at runtime any more.
    public const string EmbeddedFallbackUrl =
        "https://raw.githubusercontent.com/sweatpotato13/FFXIV-MahjongSensei/main/telemetry-disabled";

    public const string ConfigUrl =
        "https://raw.githubusercontent.com/sweatpotato13/FFXIV-MahjongSensei/main/telemetry-disabled";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Always resolves to a disabled endpoint: this fork uploads nothing and contacts no telemetry host.</summary>
    public static Task<TelemetryEndpoint> ResolveAsync(
        HttpClient http, CancellationToken ct = default)
    {
        _ = http;
        _ = ct;
        return Task.FromResult(Fallback());
    }

    private static async Task<TelemetryEndpoint> ResolveFromConfigAsync(
        HttpClient http, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var resp = await http.GetAsync(ConfigUrl, cts.Token).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var parsed = await JsonSerializer.DeserializeAsync<TelemetryEndpoint>(
                stream, JsonOpts, cts.Token).ConfigureAwait(false);

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.UploadUrl))
                return Fallback();
            return parsed;
        }
        catch
        {
            return Fallback();
        }
    }

    private static TelemetryEndpoint Fallback() =>
        new(UploadUrl: EmbeddedFallbackUrl, Enabled: false, MinPluginVersion: null);
}

public sealed record TelemetryEndpoint(
    [property: JsonPropertyName("upload_url")] string UploadUrl,
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("min_plugin_version")] string? MinPluginVersion);
