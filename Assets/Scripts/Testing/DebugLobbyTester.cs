using System.Collections.Generic;
using Steamworks.Data;
using SteamMultiplayer.Network;
using UnityEngine;

namespace SteamMultiplayer.Testing
{
    /// <summary>
    /// On-screen debug panel for testing SteamLobbyManager.
    /// Attach to any GameObject in your test scene alongside DebugNetworkTester.
    ///
    /// Panel layout:
    ///   Left column  – lobby status + action buttons
    ///   Right column – lobby list (scrollable) + event log
    ///
    /// Remove or disable when real lobby UI is ready.
    /// </summary>
    public class DebugLobbyTester : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Create Settings")]
        [Tooltip("Lobby name to use when pressing Create.")]
        [SerializeField] private string _lobbyName = "Test Room";

        [Tooltip("Max players when creating a lobby.")]
        [Range(2, 6)]
        [SerializeField] private int _maxPlayers = 6;

        [Tooltip("Visibility when creating a lobby.")]
        [SerializeField] private LobbyVisibility _visibility = LobbyVisibility.Public;

        [Header("Join By ID")]
        [Tooltip("Paste a lobby SteamId (ulong) here and press 'Join by ID'.")]
        [SerializeField] private string _joinLobbyId = "";

        [Header("GUI")]
        [SerializeField] private int _fontSize = 15;
        [SerializeField] private int _logMaxLines = 10;

        // ── State ──────────────────────────────────────────────────────────────
        private List<Lobby> _lobbyList = new();
        private Vector2 _listScroll;
        private readonly Queue<string> _eventLog = new();
        private bool _subscribed;

        // ── Panel geometry constants ───────────────────────────────────────────
        private const float PanelX      = 8f;
        private const float PanelY      = 270f;   // sits below DebugNetworkTester
        private const float LeftWidth   = 260f;
        private const float RightWidth  = 320f;
        private const float PanelHeight = 340f;
        private const float Pad         = 8f;

        // ── Unity Lifecycle ────────────────────────────────────────────────────
        private void Start()
        {
            TrySubscribe();
        }

        private void OnEnable()
        {
            TrySubscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        // ── Event Subscription ─────────────────────────────────────────────────
        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (SteamLobbyManager.Instance == null) return;

            SteamLobbyManager.Instance.OnLobbyCreated       += OnLobbyCreated;
            SteamLobbyManager.Instance.OnLobbyJoined        += OnLobbyJoined;
            SteamLobbyManager.Instance.OnLobbyLeft          += OnLobbyLeft;
            SteamLobbyManager.Instance.OnLobbyCreateFailed  += OnCreateFailed;
            SteamLobbyManager.Instance.OnLobbyJoinFailed    += OnJoinFailed;
            SteamLobbyManager.Instance.OnMemberJoined       += OnMemberJoined;
            SteamLobbyManager.Instance.OnMemberLeft         += OnMemberLeft;
            SteamLobbyManager.Instance.OnLobbyListRefreshed += OnListRefreshed;
            SteamLobbyManager.Instance.OnHostLeft           += OnHostLeft;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (SteamLobbyManager.Instance == null) return;

            SteamLobbyManager.Instance.OnLobbyCreated       -= OnLobbyCreated;
            SteamLobbyManager.Instance.OnLobbyJoined        -= OnLobbyJoined;
            SteamLobbyManager.Instance.OnLobbyLeft          -= OnLobbyLeft;
            SteamLobbyManager.Instance.OnLobbyCreateFailed  -= OnCreateFailed;
            SteamLobbyManager.Instance.OnLobbyJoinFailed    -= OnJoinFailed;
            SteamLobbyManager.Instance.OnMemberJoined       -= OnMemberJoined;
            SteamLobbyManager.Instance.OnMemberLeft         -= OnMemberLeft;
            SteamLobbyManager.Instance.OnLobbyListRefreshed -= OnListRefreshed;
            SteamLobbyManager.Instance.OnHostLeft           -= OnHostLeft;
            _subscribed = false;
        }

        // ── OnGUI ──────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            var manager = SteamLobbyManager.Instance;
            if (manager == null)
            {
                GUI.Label(
                    new Rect(PanelX, PanelY, 400, 24),
                    "[DebugLobby] SteamLobbyManager not found in scene.");
                return;
            }

            // Ensure subscribed even if SteamLobbyManager appeared after OnEnable
            TrySubscribe();

            GUIStyle boxStyle = BuildBoxStyle();

            // ── Left Panel: Status + Actions ──────────────────────────────────
            DrawLeftPanel(manager, boxStyle);

            // ── Right Panel: Lobby List + Event Log ───────────────────────────
            DrawRightPanel(manager, boxStyle);
        }

        // ── Left Panel ─────────────────────────────────────────────────────────
        private void DrawLeftPanel(SteamLobbyManager manager, GUIStyle boxStyle)
        {
            float x = PanelX;
            float y = PanelY;

            // Status box
            string status = BuildStatusText(manager);
            GUI.Box(new Rect(x, y, LeftWidth, 130), status, boxStyle);
            y += 135;

            // Buttons: only show relevant ones based on current state
            if (!manager.IsInLobby)
            {
                if (GUI.Button(new Rect(x, y, LeftWidth, 28), "Create Lobby"))
                    manager.CreateLobby(_lobbyName, _visibility, _maxPlayers);
                y += 33;

                if (GUI.Button(new Rect(x, y, LeftWidth / 2 - 2, 28), "Join by ID"))
                {
                    if (ulong.TryParse(_joinLobbyId, out ulong id))
                        manager.JoinLobbyById(id);
                    else
                        AddLog("[!] Invalid lobby ID – must be a ulong number.");
                }

                if (GUI.Button(new Rect(x + LeftWidth / 2 + 2, y, LeftWidth / 2 - 2, 28), "Refresh List"))
                    manager.RefreshLobbyList();
            }
            else
            {
                if (GUI.Button(new Rect(x, y, LeftWidth, 28), "Leave Lobby"))
                    manager.LeaveLobby();
                y += 33;

                if (GUI.Button(new Rect(x, y, LeftWidth, 28), "Refresh List"))
                    manager.RefreshLobbyList();
            }
        }

        // ── Right Panel ────────────────────────────────────────────────────────
        private void DrawRightPanel(SteamLobbyManager manager, GUIStyle boxStyle)
        {
            float x = PanelX + LeftWidth + Pad;
            float y = PanelY;

            // Lobby list header
            GUI.Box(new Rect(x, y, RightWidth, 20), "Lobby List", boxStyle);
            y += 22;

            // Scrollable lobby list
            float listHeight = 160f;
            Rect scrollView  = new Rect(x, y, RightWidth, listHeight);
            Rect content     = new Rect(0, 0, RightWidth - 20, Mathf.Max(listHeight, _lobbyList.Count * 30f));

            _listScroll = GUI.BeginScrollView(scrollView, _listScroll, content);
            for (int i = 0; i < _lobbyList.Count; i++)
            {
                Lobby lobby = _lobbyList[i];
                string lobbyName  = lobby.GetData(SteamLobbyManager.KEY_LOBBY_NAME);
                string memberInfo = $"{lobby.MemberCount}/{lobby.MaxMembers}";
                string label      = $"{lobbyName}  [{memberInfo}]";

                if (GUI.Button(new Rect(0, i * 30f, content.width, 26), label))
                {
                    if (!manager.IsInLobby)
                        manager.JoinLobby(lobby);
                    else
                        AddLog("[!] Leave your current lobby before joining another.");
                }
            }
            GUI.EndScrollView();
            y += listHeight + 4;

            // Event log
            string logText = "<b>Event Log:</b>\n";
            foreach (string line in _eventLog)
                logText += line + "\n";

            float logHeight = (_logMaxLines + 2) * (_fontSize + 4);
            GUI.Box(new Rect(x, y, RightWidth, logHeight), logText, boxStyle);
        }

        // ── Event Receivers ────────────────────────────────────────────────────
        private void OnLobbyCreated(Lobby l)
            => AddLog($"CREATED: \"{l.GetData(SteamLobbyManager.KEY_LOBBY_NAME)}\"  [{l.Id.Value}]");

        private void OnLobbyJoined(Lobby l)
            => AddLog($"JOINED: \"{l.GetData(SteamLobbyManager.KEY_LOBBY_NAME)}\"  [{l.Id.Value}]");

        private void OnLobbyLeft()
            => AddLog("LEFT lobby.");

        private void OnCreateFailed(string reason)
            => AddLog($"[!] Create failed: {reason}");

        private void OnJoinFailed(string reason)
            => AddLog($"[!] Join failed: {reason}");

        private void OnMemberJoined(Steamworks.Friend f)
            => AddLog($"+ {f.Name} joined.");

        private void OnMemberLeft(Steamworks.Friend f)
            => AddLog($"- {f.Name} left.");

        private void OnListRefreshed(List<Lobby> list)
        {
            _lobbyList = list;
            AddLog($"List refreshed: {list.Count} lobbies.");
        }

        private void OnHostLeft()
            => AddLog("[!] HOST LEFT – disconnecting.");

        // ── Helpers ───────────────────────────────────────────────────────────
        private void AddLog(string msg)
        {
            _eventLog.Enqueue(msg);
            while (_eventLog.Count > _logMaxLines)
                _eventLog.Dequeue();
        }

        private string BuildStatusText(SteamLobbyManager manager)
        {
            if (!manager.IsInLobby)
                return "<b>[Lobby Debug]</b>\nStatus: <color=#aaaaaa>Not in lobby</color>";

            Lobby l = manager.CurrentLobby.Value;
            string ownerTag = manager.IsLobbyOwner
                ? "<color=#ffdd44>[OWNER]</color>"
                : "<color=#44ccff>[MEMBER]</color>";
            return
                $"<b>[Lobby Debug]</b>\n" +
                $"Role   : {ownerTag}\n" +
                $"Name   : {l.GetData(SteamLobbyManager.KEY_LOBBY_NAME)}\n" +
                $"ID     : {l.Id.Value}\n" +
                $"Members: {l.MemberCount} / {l.MaxMembers}";
        }

        private GUIStyle BuildBoxStyle()
        {
            var s = new GUIStyle(GUI.skin.box)
            {
                fontSize  = _fontSize,
                alignment = TextAnchor.UpperLeft,
                richText  = true
            };
            s.normal.textColor = UnityEngine.Color.white;
            return s;
        }
    }
}
