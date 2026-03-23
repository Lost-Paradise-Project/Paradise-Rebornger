using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._GoobStation.CCVar;
using Content.Shared._GoobStation.StationReport;
using Robust.Shared.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;

namespace Content.Server._GoobStation.StationReportDiscordIntergrationSystem;

public sealed class StationReportDiscordIntergration : EntitySystem
{
    //thank you Timfa for writing this code
    private static readonly HttpClient client = new();
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private string? _webhookUrl;

    public override void Initialize()
    {
        base.Initialize();

        //subscribes to the endroundevent and Stationreportevent
        SubscribeLocalEvent<StationReportEvent>(OnStationReportReceived);

        // Keep track of CCVar value, update if changed
        _cfg.OnValueChanged(GoobCVars.StationReportDiscordWebHook, url => _webhookUrl = url, true);
    }

    public static string? report;

    private static readonly TagReplacement[] _replacements =
    {
        new(new Regex(@"\[/?bold\]"), @"**"),
        new(new Regex(@"\[/?italic\]"), @"_"),
        new(new Regex(@"\[/?mono\]"), @"__"),
        new(new Regex(@">"), @""),
        new(new Regex(@"\[h1\]"), @"# "),
        new(new Regex(@"\[h2\]"), @"## "),
        new(new Regex(@"\[h3\]"), @"### "),
        new(new Regex(@"\[h4\]"), @"-# "),
        new(new Regex(@"\[/h[0-9]\]"), @""),
        new(new Regex(@"\[head=1\]"), @"# "),
        new(new Regex(@"\[head=2\]"), @"## "),
        new(new Regex(@"\[head=3\]"), @"### "),
        new(new Regex(@"\[head=4\]"), @"-# "),
        new(new Regex(@"\[/head\]"), @""),
        new(new Regex(@"\[/?color(=[#0-9a-zA-Z]+)?\]"), @"")
    };

    private void OnStationReportReceived(StationReportEvent ev)
    {
        report = ev.StationReportText;

        if (string.IsNullOrWhiteSpace(report))
            return;

        foreach (var replacement in _replacements)
            report = replacement.Regex.Replace(report, replacement.Replacement);

        // Run async without blocking
        _ = SendMessageAsync(report);
    }

    private async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    description = message
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(_webhookUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Logger.Error($"Error sending station report to discord: {ex}");
        }
    }

    public struct TagReplacement
    {
        public Regex Regex;
        public string Replacement;
        public TagReplacement(Regex regex, string replacement)
        {
            Regex = regex;
            Replacement = replacement;
        }
    }
}
