using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared._LP;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using System.Net;

#if LP
using Content.Server._NC.DiscordAuth;
#endif

namespace Content.Server.Administration.Managers;

public sealed partial class BanManager : IBanManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly ServerDbEntryManager _entryManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systems = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;
    [Dependency] private readonly UserDbDataManager _userDbData = default!;
#if LP
    [Dependency] private readonly DiscordAuthManager _discordAuthManager = default!;
#endif

    private ISawmill _sawmill = default!;

    public const string SawmillId = "admin.bans";
    public const string DbTypeAntag = "Antag";
    public const string DbTypeJob = "Job";

    //LP edit start
    private readonly HttpClient _httpClient = new();
    private string _serverName = string.Empty;
    private string _webhookUrl = string.Empty;
    private WebhookData? _webhookData;
    private string _webhookName = "Банлог";
    private string _webhookAvatarUrl = "https://i.imgflip.com/6h0bjx.png";
    //LP edit end

    private readonly Dictionary<ICommonSession, List<BanDef>> _cachedRoleBans = new();
    // Cached ban exemption flags are used to handle
    private readonly Dictionary<ICommonSession, ServerBanExemptFlags> _cachedBanExemptions = new();

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgRoleBans>();

        _db.SubscribeToJsonNotification<BanNotificationData>(
            _taskManager,
            _sawmill,
            BanNotificationChannel,
            ProcessBanNotification,
            OnDatabaseNotificationEarlyFilter);

        _userDbData.AddOnLoadPlayer(CachePlayerData);
        _userDbData.AddOnPlayerDisconnect(ClearPlayerData);

        _webhookUrl = _cfg.GetCVar(LPCvars.DiscordBanWebhook);
        _serverName = _cfg.GetCVar(CCVars.ServerLobbyName);
    }

    private async Task CachePlayerData(ICommonSession player, CancellationToken cancel)
    {
        var flags = await _db.GetBanExemption(player.UserId, cancel);

        var netChannel = player.Channel;
        ImmutableArray<byte>? hwId = netChannel.UserData.HWId.Length == 0 ? null : netChannel.UserData.HWId;
        var modernHwids = netChannel.UserData.ModernHWIds;
        var roleBans = await _db.GetBansAsync(
            netChannel.RemoteEndPoint.Address,
            player.UserId,
            hwId,
            modernHwids,
            false,
            type: BanType.Role);

        var userRoleBans = new List<BanDef>();
        foreach (var ban in roleBans)
        {
            userRoleBans.Add(ban);
        }

        cancel.ThrowIfCancellationRequested();
        _cachedBanExemptions[player] = flags;
        _cachedRoleBans[player] = userRoleBans;

        SendRoleBans(player);
    }

    private void ClearPlayerData(ICommonSession player)
    {
        _cachedBanExemptions.Remove(player);
    }

    public void Restart()
    {
        // Clear out players that have disconnected.
        var toRemove = new ValueList<ICommonSession>();
        foreach (var player in _cachedRoleBans.Keys)
        {
            if (player.Status == SessionStatus.Disconnected)
                toRemove.Add(player);
        }

        foreach (var player in toRemove)
        {
            _cachedRoleBans.Remove(player);
        }

        // Check for expired bans
        foreach (var roleBans in _cachedRoleBans.Values)
        {
            roleBans.RemoveAll(ban => DateTimeOffset.Now > ban.ExpirationTime);
        }
    }

    #region Server Bans
    public async Task<int> CreateServerBan(CreateServerBanInfo banInfo)
    {
        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Server, null);

        banDef = await _db.AddBanAsync(banDef);

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules))
                await _db.SetLastReadRules(userId, null); // Reset their last read rules. They probably need a refresher!
        }

        if (_cfg.GetCVar(CCVars.ServerBanResetLastReadRules))
        {
            // Reset their last read rules. They probably need a refresher!
            foreach (var (userId, _) in banInfo.Users)
            {
                await _db.SetLastReadRules(userId, null);
            }
        }

        var adminName = banInfo.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banInfo.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        var addressRangeString = banInfo.AddressRanges.Count != 0
            ? "null"
            : string.Join(", ", banInfo.AddressRanges.Select(a => $"{a.Address}/{a.Mask}"));

        var hwidString = banInfo.HWIds.Count == 0
            ? "null"
            : string.Join(", ", banInfo.HWIds);

        var expiresString = expires == null ? Loc.GetString("server-ban-string-never") : $"{expires}";

        var key = _cfg.GetCVar(CCVars.AdminShowPIIOnBan) ? "server-ban-string" : "server-ban-string-no-pii";

        var logMessage = Loc.GetString(
            key,
            ("admin", adminName),
            ("severity", banDef.Severity),
            ("expires", expiresString),
            ("name", targetName),
            ("ip", addressRangeString),
            ("hwid", hwidString),
            ("reason", banInfo.Reason));

        _sawmill.Info(logMessage);
        _chat.SendAdminAlert(logMessage);

        KickMatchingConnectedPlayers(banDef, "newly placed ban");

        return banDef?.Id ?? 0;
    }

    private NoteSeverity GetSeverityForServerBan(CreateBanInfo banInfo, CVarDef<string> defaultCVar)
    {
        if (banInfo.Severity != null)
            return banInfo.Severity.Value;

        if (Enum.TryParse(_cfg.GetCVar(defaultCVar), true, out NoteSeverity parsedSeverity))
            return parsedSeverity;

        _sawmill.Error($"CVar {defaultCVar.Name} has invalid ban severity!");
        return NoteSeverity.None;
    }

    private void KickMatchingConnectedPlayers(BanDef def, string source)
    {
        foreach (var player in _playerManager.Sessions)
        {
            if (BanMatchesPlayer(player, def))
            {
                KickForBanDef(player, def);
                _sawmill.Info($"Kicked player {player.Name} ({player.UserId}) through {source}");
            }
        }
    }

    private bool BanMatchesPlayer(ICommonSession player, BanDef ban)
    {
        var playerInfo = new BanMatcher.PlayerInfo
        {
            UserId = player.UserId,
            Address = player.Channel.RemoteEndPoint.Address,
            HWId = player.Channel.UserData.HWId,
            ModernHWIds = player.Channel.UserData.ModernHWIds,
            // It's possible for the player to not have cached data loading yet due to coincidental timing.
            // If this is the case, we assume they have all flags to avoid false-positives.
            ExemptFlags = _cachedBanExemptions.GetValueOrDefault(player, ServerBanExemptFlags.All),
            IsNewPlayer = false,
        };

        return BanMatcher.BanMatches(ban, playerInfo);
    }

    private void KickForBanDef(ICommonSession player, BanDef def)
    {
        var message = def.FormatBanMessage(_cfg, _localizationManager);
        player.Channel.Disconnect(message);
    }

    #endregion

    #region Role Bans

    public async Task<(int, List<string>)> CreateRoleBan(CreateRoleBanInfo banInfo)
    {
        ImmutableArray<BanRoleDef> roleDefs =
        [
            .. ToBanRoleDef(banInfo.JobPrototypes),
            .. ToBanRoleDef(banInfo.AntagPrototypes),
        ];

        if (roleDefs.Length == 0)
            throw new ArgumentException("Must specify at least one role to ban!");

        var (banDef, expires) = await CreateBanDef(banInfo, BanType.Role, roleDefs);

        var banroles = await AddRoleBan(banDef);

        var length = expires == null
            ? Loc.GetString("cmd-roleban-inf")
            : Loc.GetString("cmd-roleban-until", ("expires", expires));

        var targetName = banInfo.Users.Count == 0
            ? "null"
            : string.Join(", ", banInfo.Users.Select(u => $"{u.UserName} ({u.UserId})"));

        _chat.SendAdminAlert(Loc.GetString(
            "cmd-roleban-success",
            ("target", targetName),
            ("role", string.Join(", ", roleDefs)),
            ("reason", banInfo.Reason),
            ("length", length)));

        foreach (var (userId, _) in banInfo.Users)
        {
            if (_playerManager.TryGetSessionById(userId, out var session))
                SendRoleBans(session);
        }

        return banroles;
    }

    private async Task<(BanDef Ban, DateTimeOffset? Expires)> CreateBanDef(
        CreateBanInfo banInfo,
        BanType type,
        ImmutableArray<BanRoleDef>? roleBans)
    {
        if (banInfo.Users.Count == 0 && banInfo.HWIds.Count == 0 && banInfo.AddressRanges.Count == 0)
            throw new ArgumentException("Must specify at least one user, HWID, or address range");

        DateTimeOffset? expires = null;
        if (banInfo.Duration is { } duration)
            expires = DateTimeOffset.Now + duration;

        ImmutableArray<int> roundIds;
        if (banInfo.RoundIds.Count > 0)
        {
            roundIds = [.. banInfo.RoundIds];
        }
        else if (_systems.TryGetEntitySystem<GameTicker>(out var ticker) && ticker.RoundId != 0)
        {
            roundIds = [ticker.RoundId];
        }
        else
        {
            roundIds = [];
        }

        return (new BanDef(
            null,
            type,
            [.. banInfo.Users.Select(u => u.UserId)],
            [.. banInfo.AddressRanges],
            [.. banInfo.HWIds],
            DateTimeOffset.Now,
            expires,
            roundIds,
            await GetPlayTime(banInfo),
            banInfo.Reason,
            GetSeverityForServerBan(banInfo, CCVars.ServerBanDefaultSeverity),
            banInfo.BanningAdmin,
            null,
            roles: roleBans), expires);
    }

    private async Task<TimeSpan> GetPlayTime(CreateBanInfo banInfo)
    {
        var firstPlayer = banInfo.Users.FirstOrNull()?.UserId;
        if (firstPlayer == null)
            return TimeSpan.Zero;

        return (await _db.GetPlayTimes(firstPlayer.Value))
            .Find(p => p.Tracker == PlayTimeTrackingShared.TrackerOverall)
            ?.TimeSpent ?? TimeSpan.Zero;
    }

    private IEnumerable<BanRoleDef> ToBanRoleDef<T>(IEnumerable<ProtoId<T>> protoIds) where T : class, IPrototype
    {
        return protoIds.Select(protoId =>
        {
            // TODO: I have no idea if this check is necessary. The previous code was a complete mess,
            // so out of safety I'm leaving this in.
            if (_prototypeManager.HasIndex<JobPrototype>(protoId) && _prototypeManager.HasIndex<AntagPrototype>(protoId))
            {
                throw new InvalidOperationException(
                    $"Creating role ban for {protoId}: cannot create role ban, role is both JobPrototype and AntagPrototype.");
            }

            // Don't trust the input: make sure the role actually exists.
            if (!_prototypeManager.HasIndex(protoId))
                throw new UnknownPrototypeException(protoId, typeof(T));

            return new BanRoleDef(PrototypeKindToDbType<T>(), protoId);
        });
    }

    private static string PrototypeKindToDbType<T>() where T : class, IPrototype
    {
        if (typeof(T) == typeof(JobPrototype))
            return DbTypeJob;

        if (typeof(T) == typeof(AntagPrototype))
            return DbTypeAntag;

        throw new ArgumentException($"Unknown prototype kind for role bans: {typeof(T)}");
    }

    private async Task<(int, List<string>)> AddRoleBan(BanDef banDef)
    {
        banDef = await _db.AddBanAsync(banDef);

        foreach (var user in banDef.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var player)
                && _cachedRoleBans.TryGetValue(player, out var cachedBans))
            {
                cachedBans.Add(banDef);
            }
        }

        List<string> roles = (banDef.Roles ?? ImmutableArray<BanRoleDef>.Empty)
            .Select(r => r.RoleId)
            .ToList();


        return (banDef.Id ?? 0, roles);
    }

    public async Task<string> PardonRoleBan(int banId, NetUserId? unbanningAdmin, DateTimeOffset unbanTime)
    {
        var ban = await _db.GetBanAsync(banId);

        if (ban == null)
        {
            return $"No ban found with id {banId}";
        }

        if (ban.Type != BanType.Role)
            throw new InvalidOperationException("Ban was not a role ban!");

        if (ban.Unban != null)
        {
            var response = new StringBuilder("This ban has already been pardoned");

            if (ban.Unban.UnbanningAdmin != null)
            {
                response.Append($" by {ban.Unban.UnbanningAdmin.Value}");
            }

            response.Append($" in {ban.Unban.UnbanTime}.");
            return response.ToString();
        }

        await _db.AddUnbanAsync(new UnbanDef(banId, unbanningAdmin, DateTimeOffset.Now));

        foreach (var user in ban.UserIds)
        {
            if (_playerManager.TryGetSessionById(user, out var session)
                && _cachedRoleBans.TryGetValue(session, out var roleBans))
            {
                roleBans.RemoveAll(roleBan => roleBan.Id == ban.Id);
                SendRoleBans(session);
            }

        }

        return $"Pardoned ban with id {banId}";
    }

    public HashSet<ProtoId<JobPrototype>>? GetJobBans(NetUserId playerUserId)
    {
        return GetRoleBans<JobPrototype>(playerUserId);
    }

    public HashSet<ProtoId<AntagPrototype>>? GetAntagBans(NetUserId playerUserId)
    {
        return GetRoleBans<AntagPrototype>(playerUserId);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(NetUserId playerUserId) where T : class, IPrototype
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return GetRoleBans<T>(session);
    }

    private HashSet<ProtoId<T>>? GetRoleBans<T>(ICommonSession playerSession) where T : class, IPrototype
    {
        if (!_cachedRoleBans.TryGetValue(playerSession, out var roleBans))
            return null;

        var dbType = PrototypeKindToDbType<T>();

        return roleBans
            .SelectMany(ban => ban.Roles!.Value)
            .Where(role => role.RoleType == dbType)
            .Select(role => new ProtoId<T>(role.RoleId))
            .ToHashSet();
    }

    public HashSet<BanRoleDef>? GetRoleBans(NetUserId playerUserId)
    {
        if (!_playerManager.TryGetSessionById(playerUserId, out var session))
            return null;

        return _cachedRoleBans.TryGetValue(session, out var roleBans)
            ? roleBans.SelectMany(banDef => banDef.Roles ?? []).ToHashSet()
            : null;
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<JobPrototype>> jobs)
    {
        return IsRoleBanned<JobPrototype>(player, jobs);
    }

    public bool IsRoleBanned(ICommonSession player, List<ProtoId<AntagPrototype>> antags)
    {
        return IsRoleBanned<AntagPrototype>(player, antags);
    }

    private bool IsRoleBanned<T>(ICommonSession player, List<ProtoId<T>> roles) where T : class, IPrototype
    {
        var bans = GetRoleBans(player.UserId);

        if (bans is null || bans.Count == 0)
            return false;

        var dbType = PrototypeKindToDbType<T>();

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var role in roles)
        {
            if (bans.Contains(new BanRoleDef(dbType, role)))
                return true;
        }

        return false;
    }

    public void SendRoleBans(ICommonSession pSession)
    {
        var bans = new MsgRoleBans()
        {
            JobBans = (GetRoleBans<JobPrototype>(pSession) ?? []).ToList(),
            AntagBans = (GetRoleBans<AntagPrototype>(pSession) ?? []).ToList(),
        };

        _sawmill.Debug($"Sent role bans to {pSession.Name}");
        _netManager.ServerSendMessage(bans, pSession.Channel);
    }

    #endregion

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill(SawmillId);
    }

    #region Webhook
    public async void WebhookUpdateRoleBans(CreateRoleBanInfo banInfo, int banId, List<string> roles)
    {

        SendWebhook(await GenerateJobBanPayload(banInfo, banId, roles));
    }

    public async void WebhookUpdateBans(CreateServerBanInfo banInfo, int banId)
    {

        SendWebhook(await GenerateBanPayload(banInfo, banId));
    }

    private async void SendWebhook(WebhookPayload payload)
    {
        if (_webhookUrl == string.Empty) return;

        var request = await _httpClient.PostAsync($"{_webhookUrl}?wait=true",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

        var content = await request.Content.ReadAsStringAsync();
        if (!request.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when posting message (perhaps the message is too long?): {request.StatusCode}\nResponse: {content}");
            return;
        }

        var id = JsonNode.Parse(content)?["id"];
        if (id == null)
        {
            _sawmill.Log(LogLevel.Error, $"Could not find id in json-content returned from discord webhook: {content}");
            return;
        }
    }
    private async Task<WebhookPayload> GenerateJobBanPayload(CreateRoleBanInfo banDef, int banid, List<string> roles)
    {
        var targetLink = Loc.GetString("server-ban-no-name");
        var adminLink = Loc.GetString("system-user");
        var mentions = new List<User> { };

#if LP
        if (banDef.Users.Count > 0)
        {
            foreach (var (userId, userName) in banDef.Users)
            {
                var discordId = _discordAuthManager.GetDiscordIdForPlayer(userId);
                targetLink = string.Concat(discordId, discordId != null ? $"<@{discordId}>" : Loc.GetString("server-ban-no-name-dc"));
                if (discordId != null)
                    mentions.Add(new User { Id = discordId });
            }
        }

        string? adminDiscordId = null;

        if (banDef.BanningAdmin.HasValue)
        {
            adminDiscordId = _discordAuthManager.GetDiscordIdForPlayer(banDef.BanningAdmin.Value);
        }
        adminLink = adminDiscordId != null ? $"<@{adminDiscordId}>" : Loc.GetString("system-user");

        if (adminDiscordId != null)
        {
            mentions.Add(new User { Id = adminDiscordId });
        }
#endif

        var adminName = banDef.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        string targetName = "";
        if (banDef.Users.Count > 0)
        {
            foreach (var (userId, userName) in banDef.Users)
            {
                targetName = string.Concat(targetName, "  " + (await _db.GetPlayerRecordByUserId(userId))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", userName)));
            }
        }


        var expiresString = !banDef.Duration.HasValue
            ? Loc.GetString("server-ban-string-never")
            : $"<t:{(DateTimeOffset.UtcNow + banDef.Duration.Value).ToUnixTimeSeconds()}:R>";

        var reason = banDef.Reason;
        var id = banid;
        var round = "" + string.Join("; ", banDef.RoundIds.OrderBy(x => x));
        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        if (!(banDef.RoundIds.Count > 0 || roundId == null))
        {
            round = $"{roundId}";
        }

        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = $"<t:{((DateTimeOffset)DateTimeOffset.Now.UtcDateTime).ToUnixTimeSeconds()}:R>";

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.Duration.HasValue) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
                        "server-role-ban-string",
                        ("serverName", serverName),
                        ("targetName", targetName),
                        ("targetLink", targetLink),
                        ("adminName", adminName),
                        ("adminLink", adminLink),
                        ("TimeNow", timeNow),
                        ("roles", string.Join(", ", roles)),
                        ("expiresString", expiresString),
                        ("reason", reason),
                        ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0x0042F1,
                        Author = new EmbedAuthor
                        {
                            Name = Loc.GetString("server-role-ban", ("mins", banDef.Duration.Value.TotalMinutes)) + $"",
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                        },
        },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
                        "server-perma-role-ban-string",
                        ("serverName", serverName),
                        ("targetName", targetName),
                        ("targetLink", targetLink),
                        ("adminName", adminName),
                        ("adminLink", adminLink),
                        ("TimeNow", timeNow),
                        ("roles", string.Join(", ", banDef.JobPrototypes)),
                        ("expiresString", expiresString),
                        ("reason", reason),
                        ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0xffC840,
                        Author = new EmbedAuthor
                        {
                            Name = $"{Loc.GetString("server-perma-role-ban")}",
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                        },
        },
                },
            };
    }

    private async Task<WebhookPayload> GenerateBanPayload(CreateServerBanInfo banDef, int banid)
    {
        var targetLink = Loc.GetString("server-ban-no-name");
        var adminLink = Loc.GetString("system-user");
        var mentions = new List<User> { };

#if LP
        if (banDef.Users.Count > 0)
        {
            foreach (var (userId, userName) in banDef.Users)
            {
                var discordId = _discordAuthManager.GetDiscordIdForPlayer(userId);
                targetLink = string.Concat(discordId, discordId != null ? $"<@{discordId}>" : Loc.GetString("server-ban-no-name-dc"));
                if (discordId != null)
                    mentions.Add(new User { Id = discordId });
            }
        }

        string? adminDiscordId = null;

        if (banDef.BanningAdmin.HasValue)
        {
            adminDiscordId = _discordAuthManager.GetDiscordIdForPlayer(banDef.BanningAdmin.Value);
        }
        adminLink = adminDiscordId != null ? $"<@{adminDiscordId}>" : Loc.GetString("system-user");

        if (adminDiscordId != null)
        {
            mentions.Add(new User { Id = adminDiscordId });
        }
#endif

        var adminName = banDef.BanningAdmin == null
            ? Loc.GetString("system-user")
            : (await _db.GetPlayerRecordByUserId(banDef.BanningAdmin.Value))?.LastSeenUserName ?? Loc.GetString("system-user");

        string targetName = "";
        if (banDef.Users.Count > 0)
        {
            foreach (var (userId, userName) in banDef.Users)
            {
                targetName = string.Concat(targetName, "  " + (await _db.GetPlayerRecordByUserId(userId))?.LastSeenUserName ?? Loc.GetString("server-ban-no-name", ("hwid", userName)));
            }
        }


        var expiresString = !banDef.Duration.HasValue
            ? Loc.GetString("server-ban-string-never")
            : $"<t:{(DateTimeOffset.UtcNow + banDef.Duration.Value).ToUnixTimeSeconds()}:R>";

        var reason = banDef.Reason;
        var round = "" + string.Join("; ", banDef.RoundIds.OrderBy(x => x));
        _systems.TryGetEntitySystem(out GameTicker? ticker);
        int? roundId = ticker == null || ticker.RoundId == 0 ? null : ticker.RoundId;
        if (!(banDef.RoundIds.Count > 0 || roundId == null))
        {
            round = $"{roundId}";
        }

        var severity = "" + banDef.Severity;
        var serverName = _serverName[..Math.Min(_serverName.Length, 1500)];
        var timeNow = $"<t:{((DateTimeOffset)DateTimeOffset.Now.UtcDateTime).ToUnixTimeSeconds()}:R>";

        var allowedMentions = new Dictionary<string, string[]>
        {
            { "parse", new List<string> {"users"}.ToArray() }
        };

        if (banDef.Duration.HasValue) // Time ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
                        "server-time-ban-string",
                        ("serverName", serverName),
                        ("targetName", targetName),
                        ("targetLink", targetLink),
                        ("adminName", adminName),
                        ("adminLink", adminLink),
                        ("TimeNow", timeNow),
                        ("expiresString", expiresString),
                        ("reason", reason),
                        ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0xC03045,
                        Author = new EmbedAuthor
                        {
                            Name = Loc.GetString("server-time-ban", ("mins", banDef.Duration.Value.TotalMinutes)) + $" #{banid}",
                        },
                        Footer = new EmbedFooter
                        {
                            Text =  Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                        },
        },
                },
            };
        else // Perma ban
            return new WebhookPayload
            {
                Username = _webhookName,
                AvatarUrl = _webhookAvatarUrl,
                AllowedMentions = allowedMentions,
                Mentions = mentions,
                Embeds = new List<Embed>
                {
                    new()
                    {
                        Description = Loc.GetString(
                        "server-perma-ban-string",
                        ("serverName", serverName),
                        ("targetName", targetName),
                        ("targetLink", targetLink),
                        ("adminName", adminName),
                        ("adminLink", adminLink),
                        ("TimeNow", timeNow),
                        ("reason", reason),
                        ("severity", Loc.GetString($"admin-note-editor-severity-{severity.ToLower()}"))),
                        Color = 0xCB0000,
                        Author = new EmbedAuthor
                        {
                            Name = $"{Loc.GetString("server-perma-ban")} #{banid}",
                        },
                        Footer = new EmbedFooter
                        {
                            Text = Loc.GetString("server-ban-footer", ("server", serverName), ("round", round)),
                        },
        },
                },
            };
    }

    private static readonly Regex WebhookRegex = new Regex(         //Статичный регекс быстрее обрабатывается
        @"^https://discord\.com/api/webhooks/(\d+)/((?!.*/).*)$",
        RegexOptions.Compiled);

    private void OnWebhookChanged(string url)
    {
        _webhookUrl = url;

        if (url == string.Empty)
            return;

        // Basic sanity check and capturing webhook ID and token
        var match = WebhookRegex.Match(url);

        if (!match.Success)
        {
            // TODO: Ideally, CVar validation during setting should be better integrated
            _sawmill.Warning("Webhook URL does not appear to be valid. Using anyways...");
            return;
        }

        if (match.Groups.Count <= 2)
        {
            _sawmill.Error("Could not get webhook ID or token.");
            return;
        }

        var webhookId = match.Groups[1].Value;
        var webhookToken = match.Groups[2].Value;

        // Fire and forget
        _ = SetWebhookData(webhookId, webhookToken);
    }

    private async Task SetWebhookData(string id, string token)
    {
        var response = await _httpClient.GetAsync($"https://discord.com/api/v10/webhooks/{id}/{token}");

        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _sawmill.Log(LogLevel.Error, $"Discord returned bad status code when trying to get webhook data (perhaps the webhook URL is invalid?): {response.StatusCode}\nResponse: {content}");
            return;
        }

        _webhookData = JsonSerializer.Deserialize<WebhookData>(content);
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-structure
    private struct Embed
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("color")]
        public int Color { get; set; } = 0;

        [JsonPropertyName("author")]
        public EmbedAuthor? Author { get; set; } = null;

        [JsonPropertyName("thumbnail")]
        public EmbedThumbnail? Thumbnail { get; set; } = null;

        [JsonPropertyName("footer")]
        public EmbedFooter? Footer { get; set; } = null;
        public Embed()
        {
        }
    }
    // https://discord.com/developers/docs/resources/channel#embed-object-embed-author-structure
    private struct EmbedAuthor
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedAuthor()
        {
        }
    }
    // https://discord.com/developers/docs/resources/webhook#webhook-object-webhook-structure
    private struct WebhookData
    {
        [JsonPropertyName("guild_id")]
        public string? GuildId { get; set; } = null;

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; } = null;

        public WebhookData()
        {
        }
    }
    // https://discord.com/developers/docs/resources/channel#message-object-message-structure
    private struct WebhookPayload
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; } = "";

        [JsonPropertyName("embeds")]
        public List<Embed>? Embeds { get; set; } = null;

        [JsonPropertyName("mentions")]
        public List<User> Mentions { get; set; } = new();

        [JsonPropertyName("allowed_mentions")]
        public Dictionary<string, string[]> AllowedMentions { get; set; } =
            new()
            {
                    { "parse", Array.Empty<string>() },
            };

        public WebhookPayload()
        {
        }
    }

    private struct User
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        public User()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedFooter
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        public EmbedFooter()
        {
        }
    }

    // https://discord.com/developers/docs/resources/channel#embed-object-embed-footer-structure
    private struct EmbedThumbnail
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";
        public EmbedThumbnail()
        {
        }
    }
    #endregion

    [UsedImplicitly]
    private sealed record DiscordUserResponse(string UserId, string Username);
}
