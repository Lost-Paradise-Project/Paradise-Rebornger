using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Fax;
using Robust.Shared.Configuration;

namespace Content.Server._DEN.Fax;

public sealed class FaxLoggingSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private static readonly HttpClient _httpClient = new();
    private string? _webhookUrl;

    private const int DiscordMaxDescription = 4000;

    private static readonly Regex StationGoalPattern = new(
        @"^═+\[color=#36A55D\]ЦЕЛЬ СТАНЦИИ",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly TagReplacement[] _replacements =
    {
        new(new Regex(@"\[/?bold\]"), "**"),
        new(new Regex(@"\[/?italic\]"), "_"),
        new(new Regex(@"\[/?mono\]"), "`"),
        new(new Regex(@">"), ""),
        new(new Regex(@"\[h1\]"), "# "),
        new(new Regex(@"\[h2\]"), "## "),
        new(new Regex(@"\[h3\]"), "### "),
        new(new Regex(@"\[h4\]"), "-# "),
        new(new Regex(@"\[/h[0-9]\]"), ""),
        new(new Regex(@"\[head=1\]"), "# "),
        new(new Regex(@"\[head=2\]"), "## "),
        new(new Regex(@"\[head=3\]"), "### "),
        new(new Regex(@"\[head=4\]"), "-# "),
        new(new Regex(@"\[/head\]"), ""),
        new(new Regex(@"\[/?color(=[#0-9a-zA-Z]+)?\]"), "")
    };

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("faxlogging");

        SubscribeLocalEvent<FaxSentEvent>(OnFaxSent);

        _cfg.OnValueChanged(DCCVars.DiscordFaxWebhook, url => _webhookUrl = url, true);
    }

    private void OnFaxSent(FaxSentEvent msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Content))
            return;

        if (StationGoalPattern.IsMatch(msg.Content))
        {
            _sawmill.Debug("Системный факс проигнорирован (Цель станции).");
            return;
        }

        _ = SendFaxToDiscord(msg);
    }

    private async Task SendFaxToDiscord(FaxSentEvent msg)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        var content = msg.Content;
        foreach (var replacement in _replacements)
            content = replacement.Regex.Replace(content, replacement.Replacement);

        if (content.Length > DiscordMaxDescription)
            content = content[..DiscordMaxDescription] + "... (truncated)";

        var stamps = msg.StampedBy.Count > 0 ? string.Join(", ", msg.StampedBy) : "None";

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"📠 Исходящий факс от {msg.DestinationAddress}",
                    description = content,
                    color = 3447003,
                    footer = new
                    {
                        text = $"Печати: {stamps}"
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_webhookUrl, httpContent);

            if (!response.IsSuccessStatusCode)
                _sawmill.Error($"Discord webhook returned error: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error sending fax to discord webhook: {ex}");
        }
    }

    public struct TagReplacement(Regex regex, string replacement)
    {
        public Regex Regex = regex;
        public string Replacement = replacement;
    }
}
