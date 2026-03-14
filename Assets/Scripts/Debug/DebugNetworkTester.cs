using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Transporting;
using SteamMultiplayer.Network;
using UnityEngine;

namespace SteamMultiplayer.Testing // 自己另外开了一个调试空间，如果使用.Debug可能跟 UnityEngine.Debug 冲突，导致日志无法输出
{
    /// <summary>
    /// On-screen debug panel for testing GameNetworkManager on a single machine.
    ///
    /// Keyboard shortcuts:
    ///   H  = Start Host  (server + local host-client, no second machine needed)
    ///   X  = Stop All Connections
    ///   C  = Start Client (remote, requires Host Steam ID filled in Inspector)
    ///
    /// The on-screen GUI shows:
    ///   - Current role (Host / Server-only / Client-only / Disconnected)
    ///   - Local Steam ID
    ///   - Server connection state
    ///   - Client connection state
    ///   - Number of connected remote clients (server side)
    ///   - Recent event log
    ///
    /// Remove this script when real UI is ready.
    /// </summary>
    public class DebugNetworkTester : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Remote Client Test (optional)")]
        [Tooltip("Steam ID of the Host to connect to as a remote client (press C).")]
        [SerializeField] private string _hostSteamId = "";

        [Header("GUI Settings")]
        [SerializeField] private int _guiFontSize = 16;
        [SerializeField] private int _logMaxLines = 8;

        // ── State ──────────────────────────────────────────────────────────────
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        private int _remoteClientCount = 0;
        private readonly Queue<string> _eventLog = new();

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void OnEnable()
        {
            // Wait until GameNetworkManager is available before subscribing
            if (GameNetworkManager.Instance != null)
                Subscribe();
        }

        private void Start()
        {
            // GameNetworkManager may not exist in OnEnable if it's on the same GO
            if (GameNetworkManager.Instance != null)
                Subscribe();
        }

        private void OnDisable()
        {
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.OnServerStateChanged -= OnServerState;
                GameNetworkManager.Instance.OnClientStateChanged -= OnClientState;
                GameNetworkManager.Instance.OnRemoteClientStateChanged -= OnRemoteClient;
            }
        }

        // ── Input ──────────────────────────────────────────────────────────────
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                AddLog(">> StartHost()");
                GameNetworkManager.Instance.StartHost();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                AddLog(">> StopConnection()");
                GameNetworkManager.Instance.StopConnection();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                if (string.IsNullOrWhiteSpace(_hostSteamId))
                {
                    AddLog("[!] Host Steam ID is empty – fill it in Inspector");
                    return;
                }
                AddLog($">> StartClient({_hostSteamId})");
                GameNetworkManager.Instance.StartClient(_hostSteamId);
            }
        }

        // ── On-Screen GUI ──────────────────────────────────────────────────────
        private void OnGUI()
        {
            var gnm = GameNetworkManager.Instance;
            if (gnm == null)
            {
                GUI.Label(new Rect(10, 10, 400, 24), "[DebugTester] GameNetworkManager not found");
                return;
            }

            // Build style
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = _guiFontSize,
                alignment = TextAnchor.UpperLeft,
                richText = true
            };
            style.normal.textColor = Color.white;

            // ── Role line ──
            string role = "Disconnected";
            if (gnm.IsHost)        role = "<color=#55ff55>HOST (Server + Client)</color>";
            else if (gnm.IsServer) role = "<color=#ffdd44>SERVER only</color>";
            else if (gnm.IsClient) role = "<color=#44ccff>CLIENT only</color>";

            // ── Steam ID ──
            string steamId = "–";
            var transport = gnm.FishNetManager?.TransportManager?.Transport;
            if (transport is FishyFacepunch.FishyFacepunch fp && fp.LocalUserSteamID != 0)
                steamId = fp.LocalUserSteamID.ToString();

            // ── Build text ──
            string text =
                $"<b>[Debug Network Tester]</b>\n" +
                $"Role       : {role}\n" +
                $"Steam ID   : {steamId}\n" +
                $"Server     : {_serverState}\n" +
                $"Client     : {_clientState}\n" +
                $"Remote Clients : {_remoteClientCount}\n" +
                "\n<b>Hotkeys:</b>  [H] StartHost   [C] StartClient   [X] Stop\n" +
                "\n<b>Event Log:</b>\n";

            foreach (var line in _eventLog)
                text += line + "\n";

            // ── Draw panel ──
            float width = 420;
            int lines = 10 + _eventLog.Count;
            float height = lines * (_guiFontSize + 4) + 16;
            GUI.Box(new Rect(8, 8, width, height), text, style);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private void Subscribe()
        {
            GameNetworkManager.Instance.OnServerStateChanged  += OnServerState;
            GameNetworkManager.Instance.OnClientStateChanged  += OnClientState;
            GameNetworkManager.Instance.OnRemoteClientStateChanged += OnRemoteClient;
        }

        private void OnServerState(LocalConnectionState state)
        {
            _serverState = state;
            AddLog($"[Server] → {state}");
            // Reset remote count when server stops
            if (state == LocalConnectionState.Stopped)
                _remoteClientCount = 0;
        }

        private void OnClientState(LocalConnectionState state)
        {
            _clientState = state;
            AddLog($"[Client] → {state}");
        }

        private void OnRemoteClient(NetworkConnection conn, RemoteConnectionState state)
        {
            if (state == RemoteConnectionState.Started)
            {
                _remoteClientCount++;
                AddLog($"[Remote] Client {conn.ClientId} connected  (total: {_remoteClientCount})");
            }
            else
            {
                _remoteClientCount = Mathf.Max(0, _remoteClientCount - 1);
                AddLog($"[Remote] Client {conn.ClientId} disconnected (total: {_remoteClientCount})");
            }
        }

        private void AddLog(string msg)
        {
            _eventLog.Enqueue(msg);
            while (_eventLog.Count > _logMaxLines)
                _eventLog.Dequeue();
        }
    }
}

