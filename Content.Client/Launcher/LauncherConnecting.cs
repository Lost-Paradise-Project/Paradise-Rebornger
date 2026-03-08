using System;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
// LP edit start
using Robust.Shared.ContentPack;
using System.IO;
using Content.Shared.CCVar;
using Nett;
// LP edit end

namespace Content.Client.Launcher
{
    public sealed class LauncherConnecting : Robust.Client.State.State
    {
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;
        [Dependency] private readonly IClientNetManager _clientNetManager = default!;
        [Dependency] private readonly IGameController _gameController = default!;
        [Dependency] private readonly IBaseClient _baseClient = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IClipboardManager _clipboard = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly IResourceManager _resourceManager = default!; // LP edit

        private LauncherConnectingGui? _control;
        private ISawmill _sawmill = default!;

        private Page _currentPage;
        private string? _connectFailReason;

        public string? Address => _gameController.LaunchState.Ss14Address ?? _gameController.LaunchState.ConnectAddress;

        public string? ConnectFailReason
        {
            get => _connectFailReason;
            private set
            {
                _connectFailReason = value;
                ConnectFailReasonChanged?.Invoke(value);
            }
        }

        public string? LastDisconnectReason => _baseClient.LastDisconnectReason;

        public Page CurrentPage
        {
            get => _currentPage;
            private set
            {
                _currentPage = value;
                PageChanged?.Invoke(value);
            }
        }

        public ClientConnectionState ConnectionState => _clientNetManager.ClientConnectState;

        public event Action<Page>? PageChanged;
        public event Action<string?>? ConnectFailReasonChanged;
        public event Action<ClientConnectionState>? ConnectionStateChanged;
        public event Action<NetConnectFailArgs>? ConnectFailed;

        protected override void Startup()
        {
            _sawmill = _logManager.GetSawmill("launcher-ui");
            // LP edit start

            // Load LP config
            const string configPath = "/ConfigPresets/LP/lp.toml";
            if (_resourceManager.TryContentFileRead(configPath, out var file))
            {
                // Read the entire config file as text
                using var reader = new StreamReader(file);
                var configText = reader.ReadToEnd();

                // Extract only the [infolinks] section
                var infolinksSection = ExtractInfolinksSection(configText);

                // Convert the extracted section to a stream and load it
                using var infolinksStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(infolinksSection));
                _cfg.LoadDefaultsFromTomlStream(infolinksStream);

                _sawmill.Info($"Loaded config preset: {configPath}");
            }
            else
            {
                _sawmill.Error($"Unable to load config preset: {configPath}");
            }

            _control = new LauncherConnectingGui(this, _random, _prototypeManager, _cfg, _clipboard);
            // LP edit end
            _userInterfaceManager.StateRoot.AddChild(_control);

            _clientNetManager.ConnectFailed += OnConnectFailed;
            _clientNetManager.ClientConnectStateChanged += OnConnectStateChanged;

            CurrentPage = Page.Connecting;
        }

        protected override void Shutdown()
        {
            _control?.Dispose();

            _clientNetManager.ConnectFailed -= OnConnectFailed;
            _clientNetManager.ClientConnectStateChanged -= OnConnectStateChanged;
        }

        private void OnConnectFailed(object? _, NetConnectFailArgs args)
        {
            if (args.RedialFlag)
            {
                // We've just *attempted* to connect and we've been told we need to redial, so do it.
                // Result deliberately discarded.
                Redial();
            }
            ConnectFailReason = args.Reason;
            CurrentPage = Page.ConnectFailed;
            ConnectFailed?.Invoke(args);
        }

        private void OnConnectStateChanged(ClientConnectionState state)
        {
            ConnectionStateChanged?.Invoke(state);
        }

        public void RetryConnect()
        {
            if (_gameController.LaunchState.ConnectEndpoint != null)
            {
                _baseClient.ConnectToServer(_gameController.LaunchState.ConnectEndpoint);
                CurrentPage = Page.Connecting;
            }
        }

        public bool Redial()
        {
            try
            {
                if (_gameController.LaunchState.Ss14Address != null)
                {
                    _gameController.Redial(_gameController.LaunchState.Ss14Address);
                    return true;
                }
                else
                {
                    _sawmill.Info($"Redial not possible, no Ss14Address");
                }
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Redial exception: {ex}");
            }
            return false;
        }

        public void Exit()
        {
            _gameController.Shutdown("Exit button pressed");
        }

        public void SetDisconnected()
        {
            CurrentPage = Page.Disconnected;
        }

        // LP edit start
        private string ExtractInfolinksSection(string configText)
        {
            var infolinksStart = configText.IndexOf("[infolinks]", StringComparison.Ordinal);
            if (infolinksStart == -1)
                return "";

            var infolinksEnd = configText.IndexOf("\n[", infolinksStart, StringComparison.Ordinal);
            if (infolinksEnd == -1)
                infolinksEnd = configText.Length;

            return configText.Substring(infolinksStart, infolinksEnd - infolinksStart);
        }
        // LP edit end

        public enum Page : byte
        {
            Connecting,
            ConnectFailed,
            Disconnected,
        }
    }
}
