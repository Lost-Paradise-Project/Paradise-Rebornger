using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared.Corvax.CCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server.Corvax.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration
        {
            LabelNames = new[] { "type" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    private static readonly Gauge CachedCount = Metrics.CreateGauge(
        "tts_cached_count",
        "Amount of cached TTS audio.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, byte[]> _cache = new();
    private readonly List<string> _cacheKeysSeq = new();
    private readonly Dictionary<string, Task<byte[]?>> _pendingRequests = new(); // LP edit - dedup in-flight requests
    private int _maxCachedCount = 200;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _maxCachedCount = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v => _apiToken = v, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <param name="effect">An effect to apply to the voice</param>
    /// <returns>OGG audio bytes</returns>
    // LP edit start - removed pitch, rate params (not in API); added in-flight request dedup
    public async Task<byte[]?> ConvertTextToSpeech(
        string speaker,
        string text,
        string? effect = null)
    {
        if (string.IsNullOrWhiteSpace(_apiUrl))
        {
            _sawmill.Log(LogLevel.Error, "TTS Api url not specified");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_apiToken))
        {
            _sawmill.Log(LogLevel.Error, "TTS ApiKey not specified");
            return null;
        }

        WantedCount.Inc();

        var cacheKey = GenerateCacheKey(speaker, text, effect);
        if (_cache.TryGetValue(cacheKey, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
            return data;
        }

        // If a request for the same key is already in-flight, await it instead of making a duplicate API call.
        // This prevents rate-limiting (429) when radio TTS fires HandleRadio for each receiver concurrently.
        if (_pendingRequests.TryGetValue(cacheKey, out var pendingTask))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Awaiting pending request for '{text}' speech by '{speaker}' speaker");
            return await pendingTask;
        }

        var task = FetchFromApi(speaker, text, effect, cacheKey);
        _pendingRequests[cacheKey] = task;

        try
        {
            return await task;
        }
        finally
        {
            _pendingRequests.Remove(cacheKey);
        }
    }

    private async Task<byte[]?> FetchFromApi(string speaker, string text, string? effect, string cacheKey)
    {
        var request = CreateHttpRequest(_apiUrl, _apiToken, new GenerateVoiceRequest
        {
            Text = text,
            Speaker = speaker,
            Effect = effect
        });

        var reqTime = DateTime.UtcNow;
        try
        {
            var timeout = _cfg.GetCVar(CCCVars.TTSApiTimeout);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited");
                    return null;
                }
                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                return null;
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(cts.Token);

            if (_cache.TryAdd(cacheKey, soundData))
            {
                _cacheKeysSeq.Add(cacheKey);
                if (_cache.Count > _maxCachedCount)
                {
                    var firstKey = _cacheKeysSeq.First();
                    _cache.Remove(firstKey);
                    _cacheKeysSeq.Remove(firstKey);
                }
                CachedCount.Set(_cache.Count);
            }

            _sawmill.Debug(
                $"Generated new sound for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");

            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Timeout of request generation new sound for '{text}' speech by '{speaker}' speaker");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Warning($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            return null;
        }
    }
    // LP edit end

    private static HttpRequestMessage CreateHttpRequest(string url, string apiKey, GenerateVoiceRequest body)
    {
        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["speaker"] = body.Speaker;
        query["text"] = body.Text;
        query["ext"] = "ogg"; // LP edit - removed pitch, rate, file params
        if (body.Effect != null)
            query["effect"] = body.Effect;

        uriBuilder.Query = query.ToString();
        var request = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(uriBuilder.ToString()),
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", apiKey),
            },
        };

        return request;
    }

    public void ResetCache()
    {
        _cache.Clear();
        _cacheKeysSeq.Clear();
        _pendingRequests.Clear(); // LP edit
        CachedCount.Set(0);
    }

    // LP edit start - removed pitch, rate from cache key
    private string GenerateCacheKey(string speaker, string text, string? effect)
    {
        var key = $"{speaker}/{text}/{effect ?? ""}";
    // LP edit end
        var keyData = Encoding.UTF8.GetBytes(key);
        var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(keyData);
        return Convert.ToHexString(bytes);
    }

    // LP edit start - removed Pitch, Rate
    private record GenerateVoiceRequest
    {
        public string Text { get; set; } = default!;
        public string Speaker { get; set; } = default!;
        public string? Effect { get; set; }
    }
    // LP edit end
}
