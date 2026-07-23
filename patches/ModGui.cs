using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using TMPro;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;

namespace MegabonkCommunityPatch;

public class ModGui : MonoBehaviour
{
    public ModGui(System.IntPtr ptr) : base(ptr) { }

    internal static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> MainThread = new();

    private bool _visible;
    private bool _authenticated;
    private bool _promptVisible;
    private string _passwordInput = "";
    private const string Password = "kittens";

    // damage chart
    internal static bool ChartDisabled;
    internal static bool NeedVersionPatch;
    internal static bool UpdateAvailable;
    internal static System.Collections.Generic.List<string> UnauthorizedMods = new();
    internal static bool CheatsUsed;
    internal static bool  ShowHitboxes    = false;
    internal static bool  ModMenuEnabled   = true; // toggled OFF by default in config; enable under Settings
    internal static bool  SuppressMovementInput;
    internal static float BombusAnnounceTime = -999f; // Time.time when BOMBUS spawned
    private Material  _hitboxMat;
    private Collider[] _cachedColliders;
    private int        _colliderCacheFrame = -999;
    internal static float HitboxMaxDist = 100f;
    private GameObject _statsParent;
    private GameObject _damageWindow;
    private GameObject _statsWindowObj;
    private GameObject _questsWindow;
    private UnityEngine.UI.ScrollRect _damageScroll; // chart scroll (driven manually mid-game)
    // Mid-game chart REUSES the death-screen StatsWindows object. Track what we
    // mutate so it can be fully reverted when the player actually dies.
    private bool    _chartMutated;
    private bool    _addedCanvas;
    private Vector2 _origAnchorMin, _origAnchorMax, _origPivot, _origAnchoredPos;
    private bool    _origRtCaptured;

    private readonly GuiWindowFrame _frame = new(new Vector2(20f, 20f));
    private readonly GuiWindowFrame _noticeFrame = new(new Vector2(700f, 8f));
    private float _lastWinH = 480f;
    private float _lastNoticeH = 36f;

    private bool _powerupOpen  = true;
    private bool _guiInit;
    internal static bool  GodMode      = false;
    internal static bool  FlyMode      = false;
    internal static float FlySpeed     = 20f;
    internal static bool  InstaKill    = false;
    internal static bool  FreezeEnemies = false;

    private const float FastFallSpeed = 22f;   // terminal downward speed we pull toward
    private const float FastFallAccel = 100f;  // how hard we accelerate down (units/s²)

    private const float WinW  = 440f;
    private const float PadX  = 8f;
    private const float LineH = 22f;

    private int _versionPatchFrames;
    private int _reevalFrame;

    private void Start()
    {
    }

    private void Update()
    {
        while (MainThread.TryDequeue(out var action))
            try { action(); } catch { }

        NativeSettings.TickCapture();
        NativeSettings.PollKeycapCapture();

        if (NeedVersionPatch && _versionPatchFrames++ >= 2)
        {
            string gameVer = UnityEngine.Application.version;
            var all = UnityEngine.Object.FindObjectsOfType<TMPro.TextMeshProUGUI>();
            foreach (var t in all)
            {
                if (t.text != null && t.text.Contains(gameVer))
                {
                    var attr = (BepInEx.BepInPlugin)System.Attribute.GetCustomAttribute(typeof(Plugin), typeof(BepInEx.BepInPlugin));
                    string modVer = attr?.Version?.ToString() ?? "?";
                    t.text = $"Mod v{modVer}  |  " + t.text;
                    NeedVersionPatch = false;
                    break;
                }
            }
        }

        // Reevaluate auto-conditions only every 15 frames, not every frame
        if (++_reevalFrame >= 15)
        {
            _reevalFrame = 0;
            PatchModules.ReevaluateAll();
        }

        ModMenuEnabled = Hotkeys.ModMenuEnabled.Value;

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.ModMenu.Value) && ModMenuEnabled)
        {
            if (_authenticated)
            {
                _visible = !_visible;
            }
            else
            {
                _promptVisible = !_promptVisible;
                _passwordInput = "";
            }
        }

        if (_promptVisible && !_authenticated)
        {
            SuppressMovementInput = true;
            // Mute game keyboard input while password prompt is open
            Input.ResetInputAxes();
            foreach (char c in Input.inputString)
            {
                if (c == '\b')
                {
                    if (_passwordInput.Length > 0)
                        _passwordInput = _passwordInput.Substring(0, _passwordInput.Length - 1);
                }
                else if (c == '\n' || c == '\r')
                {
                    if (_passwordInput == Password)
                    {
                        _authenticated = true;
                        _promptVisible = false;
                        _visible       = true;
                    }
                    else
                    {
                        _passwordInput = "";
                    }
                }
                else
                {
                    _passwordInput += c;
                }
            }
        }
        else
        {
            SuppressMovementInput = false;
        }

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.DamageChart.Value) && !ChartDisabled)
            ToggleDamageChart();

        // WATCHDOG: the render-on-top Canvas we add lives on the SHARED death-screen
        // object. If the player dies (ChartDisabled set in GameManager.OnDied) while
        // our Canvas is still on it, that elevated panel would raycast-block the
        // death screen's Continue button. Strip it every frame death is active so the
        // death screen can never inherit our sorting, regardless of event ordering.
        if (ChartDisabled && _chartMutated)
            RestoreForDeath();

        // Manually drive damage-chart scroll — the death-screen ScrollRect gets no
        // wheel events mid-game, so players reported it as unscrollable.
        if (_damageScroll != null && _statsParent != null && _statsParent.activeSelf)
        {
            float sd = Input.mouseScrollDelta.y;
            if (sd != 0f)
            {
                try
                {
                    _damageScroll.verticalNormalizedPosition =
                        Mathf.Clamp01(_damageScroll.verticalNormalizedPosition + sd * 0.12f);
                }
                catch { }
            }
        }

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.ChaosMenu.Value))
            ChaosMenu.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.MapScanner.Value))
            MapScanner.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.MapScanToggle.Value))
            MapScanner.ToggleScan();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.EffectsOpacity.Value))
            EffectsOpacityToggle.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.GameSpeed.Value))
            GameSpeedToggle.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.SkipChest.Value))
            SkipChestSync.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.SmartTargeting.Value))
            SmartTargeting.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.AutoUpgrade.Value))
            AutoLevelPick.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.AutoUpgradeLog.Value))
            AutoLevelPick.ToggleWindow();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.PowerupTracker.Value))
            PowerupTracker.Toggle();

        if (!Hotkeys.Suppressed && Input.GetKeyDown(Hotkeys.ChestOddsTracker.Value))
            ChestOddsTracker.Toggle();

        MapScanner.Tick();
        SkipChestSync.Tick();
        GameSpeedToggle.Tick();

        try
        {
            var pm = PlayerMovement.Instance;
            if (pm != null && !FlyMode)
            {
                unsafe
                {
                    bool onGround = *(byte*)(pm.Pointer + 0xDF) != 0;
                    bool active  = !onGround && Input.GetKey(Hotkeys.FastFall.Value);

                    if (active)
                    {
                        var rb = pm.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            var v = rb.velocity;
                            // Accelerate toward terminal downward speed instead of waiting
                            // for the apex. Responsive while still rising (decelerates the
                            // ascent immediately), but it's an acceleration (MoveTowards),
                            // not a hard velocity clamp — so it never snaps upward velocity
                            // to zero and bunny hops stay smooth. Don't slow anyone already
                            // falling faster than terminal. Horizontal velocity is left
                            // untouched so diagonal bunny-hop speed tech still works.
                            const float target = -FastFallSpeed;
                            if (v.y > target)
                            {
                                v.y = Mathf.MoveTowards(v.y, target, FastFallAccel * Time.deltaTime);
                                rb.velocity = v;
                            }
                        }
                    }
                }
            }
        }
        catch { }

        // ── Fly mode ──
        try
        {
            var pm = PlayerMovement.Instance;
            if (pm != null && FlyMode)
            {
                var rb = pm.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.useGravity = false;
                    unsafe
                    {
                        float inputX = *(float*)(pm.Pointer + 0x118);
                        float inputY = *(float*)(pm.Pointer + 0x11C);
                        var orientation = pm.orientation;
                        Vector3 wishDir = Vector3.zero;
                        if (orientation != null)
                            wishDir = orientation.forward * inputY + orientation.right * inputX;
                        float vert = 0f;
                        if (Input.GetKey(KeyCode.Space))       vert =  FlySpeed;
                        if (Input.GetKey(KeyCode.LeftControl)) vert = -FlySpeed;
                        rb.velocity = wishDir.normalized * FlySpeed + new Vector3(0f, vert, 0f);
                    }
                }
            }
            else if (pm != null && !FlyMode)
            {
                var rb = pm.GetComponent<Rigidbody>();
                if (rb != null && !rb.useGravity)
                    rb.useGravity = true;
            }
        }
        catch { }

        // Drag / resize the open menus (raw-input hit-testing, matrix-independent).
        if (UpdateAvailable || UnauthorizedMods.Count > 0 || (CheatsUsed && Assets.Scripts.Managers.EnemyManager.Instance != null))
            _noticeFrame.HandleInput(520f, _lastNoticeH, 18f);
        if (_visible)
            _frame.HandleInput(WinW, _lastWinH, LineH + 4f);
        if (ChaosMenu.Visible)
            ChaosMenu.HandleInput();
        if (MapScanner.Visible)
            MapScanner.HandleInput();
        if (AutoLevelPick.Visible)
            AutoLevelPick.HandleInput();
        if (PowerupTracker.Visible)
            PowerupTracker.HandleInput();
        if (ChestOddsTracker.Visible)
            ChestOddsTracker.HandleInput();

        // Leaderboard tooltip hover detection
        try
        {
            var lbUi = LeaderboardInjector.LbUi;
            if (lbUi != null)
                LeaderboardTooltip.CheckHover(lbUi, Patch_LBTypeSelected.CurrentTab);
        }
        catch { }
    }

    internal void ResetChartCache()
    {
        _statsParent    = null;
        _chartMutated   = false;
        _addedCanvas    = false;
        _origRtCaptured = false;
        _damageScroll   = null;
    }

    // Player died. The death screen reuses the SAME StatsWindows object our
    // mid-game chart mutates. If we toggled the chart this run, revert every
    // mutation so the vanilla death screen renders correctly and its Confirm
    // button is clickable. Also force a fresh damage-source repopulate: the
    // game's GameOverDamageSourcesUi.Start fires only once, and our mid-game
    // F2 already consumed it, so without this the panel shows stale rows.
    internal void RestoreForDeath()
    {
        try
        {
            if (!_chartMutated || _statsParent == null)
                return;

            // re-enable the sibling windows we hid (W_Stats holds Confirm)
            if (_statsWindowObj != null) _statsWindowObj.SetActive(true);
            if (_questsWindow   != null) _questsWindow.SetActive(true);

            // restore original transform
            var rt = _statsParent.GetComponent<RectTransform>();
            if (rt != null && _origRtCaptured)
            {
                rt.anchorMin        = _origAnchorMin;
                rt.anchorMax        = _origAnchorMax;
                rt.pivot            = _origPivot;
                rt.anchoredPosition = _origAnchoredPos;
            }

            // drop our custom canvas sorting
            var cv = _statsParent.GetComponent<Canvas>();
            if (cv != null)
            {
                cv.overrideSorting = false;
                if (_addedCanvas) UnityEngine.Object.Destroy(cv);
            }

            // force fresh damage rows (Start won't auto-run a 2nd time)
            var chart = _statsParent.GetComponentInChildren<GameOverDamageSourcesUi>(true);
            if (chart != null) chart.Start();

            _damageScroll   = null;
            _chartMutated   = false;
            _addedCanvas    = false;
            _origRtCaptured = false;
        }
        catch { }
    }

    private void ToggleDamageChart()
    {
        try
        {
            if (_statsParent == null)
            {
                _statsParent  = GameObject.Find("GameUI/GameUI/DeathScreen/StatsWindows");
                if (_statsParent != null)
                {
                    _damageWindow  = _statsParent.transform.Find("W_Damage")?.gameObject;
                    _statsWindowObj = _statsParent.transform.Find("W_Stats")?.gameObject;
                    _questsWindow  = _statsParent.transform.Find("W_Quests")?.gameObject;
                }
            }
            if (_statsParent == null) return;

            bool show = !_statsParent.activeSelf;
            _statsParent.SetActive(show);
            if (_statsWindowObj != null) _statsWindowObj.SetActive(!show);
            if (_questsWindow   != null) _questsWindow.SetActive(!show);

            if (show)
            {
                var chart = _statsParent.GetComponentInChildren<GameOverDamageSourcesUi>();
                if (chart != null) chart.Start();

                _chartMutated = true;

                var rt = _statsParent.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (!_origRtCaptured)
                    {
                        _origAnchorMin   = rt.anchorMin;
                        _origAnchorMax   = rt.anchorMax;
                        _origPivot       = rt.pivot;
                        _origAnchoredPos = rt.anchoredPosition;
                        _origRtCaptured  = true;
                    }
                    rt.anchorMin        = new Vector2(0f, 0.5f);
                    rt.anchorMax        = new Vector2(0f, 0.5f);
                    rt.pivot            = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(20f, 0f);
                }

                // Render above the pause menu (its own canvas out-sorts GameUI) so
                // the chart stays readable while paused.
                var cv = _statsParent.GetComponent<Canvas>();
                if (cv == null) { cv = _statsParent.AddComponent<Canvas>(); _addedCanvas = true; }
                cv.overrideSorting = true;
                cv.sortingOrder    = 30000;

                // The death-screen ScrollRect doesn't receive mouse-wheel events
                // mid-game (no active EventSystem routing to this canvas), so grab
                // it and drive scrolling ourselves in Update().
                _damageScroll = null;
                var srTf = _statsParent.transform.Find(
                    "W_Damage/WindowLayers/Content/ScrollRect");
                if (srTf != null)
                {
                    var sr = srTf.GetComponent<UnityEngine.UI.ScrollRect>();
                    if (sr != null)
                    {
                        sr.vertical   = true;
                        sr.horizontal = false;
                        sr.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
                        var content = sr.content;
                        if (content != null)
                        {
                            UnityEngine.UI.LayoutRebuilder
                                .ForceRebuildLayoutImmediate(content);
                            sr.verticalNormalizedPosition = 1f; // start at top
                        }
                        _damageScroll = sr;
                    }
                }
            }
            else
            {
                _damageScroll = null;
                // Restore the transform we moved, so a later death screen is laid out
                // correctly even if the player closed the chart before dying.
                var rt = _statsParent.GetComponent<RectTransform>();
                if (rt != null && _origRtCaptured)
                {
                    rt.anchorMin        = _origAnchorMin;
                    rt.anchorMax        = _origAnchorMax;
                    rt.pivot            = _origPivot;
                    rt.anchoredPosition = _origAnchoredPos;
                }
                // Fully remove our Canvas (not just toggle) so the shared object is
                // left pristine — no leftover sorting context that could block clicks.
                var cv = _statsParent.GetComponent<Canvas>();
                if (cv != null)
                {
                    cv.overrideSorting = false;
                    if (_addedCanvas) UnityEngine.Object.Destroy(cv);
                }
                _addedCanvas    = false;
                _chartMutated   = false;
                _origRtCaptured = false;
            }
        }
        catch { }
    }

    private void OnGUI()
    {
        // Warm up IMGUI and register windows on the very first frame,
        // not on the frame the user presses F1. This prevents the
        // first-menu-open hitch.
        if (!_guiInit)
        {
            _noticeFrame.Persist("Notices");
            _frame.Persist("ModMenu");
            _guiInit = true;

            // Pre-warm IL2CPP generic instantiation for dictionary iteration
            try
            {
                var dummyFx = new System.Collections.Generic.Dictionary<Assets.Scripts.Inventory__Items__Pickups.Stats.EStatusEffect, Assets.Scripts.Inventory__Items__Pickups.Stats.StatusEffect>();
                foreach (var kv in dummyFx) System.GC.KeepAlive(kv);
            }
            catch { }
        }

        ChaosMenu.Draw();
        MapScanner.Draw();
        AutoLevelPick.Draw();
        PowerupTracker.Draw();
        ChestOddsTracker.Draw();
        Toast.Draw();

        if (UpdateAvailable || UnauthorizedMods.Count > 0 || (CheatsUsed && Assets.Scripts.Managers.EnemyManager.Instance != null))
        {
            var savedNoticeMatrix = _noticeFrame.Begin();
            float nx = _noticeFrame.Pivot.x;
            float ny = _noticeFrame.Pivot.y;
            float nw = 520f;

            var lines = new System.Collections.Generic.List<string>();
            if (UpdateAvailable)
                lines.Add("!! MegaBonk Mod is outdated \u2014 please update to the latest version !!");
            if (UnauthorizedMods.Count > 0)
                lines.Add("!! Other mods detected \u2014 leaderboard disabled: " + string.Join(", ", UnauthorizedMods));
            if (CheatsUsed && Assets.Scripts.Managers.EnemyManager.Instance != null)
                lines.Add("!! Mod menu used \u2014 leaderboard disabled for this run");

            float nh = 24f * lines.Count + 4f;
        UiTheme.Backdrop(new Rect(nx, ny, nw, nh), "Notices");
            GUI.Box(new Rect(nx, ny, nw, nh), "Notices");
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(nx + 8f, ny + 20f + i * 24f, nw - 16f, 20f), lines[i]);

            _lastNoticeH = nh;
            _noticeFrame.End(savedNoticeMatrix);
            _noticeFrame.DrawGrip(nw, nh);
        }

        if (_promptVisible && !_authenticated)
        {
            float pw = 240f, ph = 72f;
            float px = (Screen.width  - pw) / 2f;
            float py = (Screen.height - ph) / 2f;
            GUI.Box(new Rect(px, py, pw, ph), "Mod Menu Password");
            GUI.Label(new Rect(px + 8f, py + 26f, pw - 16f, 22f), new string('*', _passwordInput.Length));
            GUI.Label(new Rect(px + 8f, py + 50f, pw - 16f, 18f), "Type password + Enter");
            return;
        }

        if (!_visible) return;

        // Leaderboard tooltip (independent of mod menu visibility)
        try { LeaderboardTooltip.OnGUI(); } catch { }

        var savedMatrix = _frame.Begin();
        float wx = _frame.Pivot.x;
        float wy = _frame.Pivot.y;
        float cw = WinW - PadX * 2f;
        float lx = wx + PadX;
        float indentX = lx + 12f;
        float indentW = cw - 12f;

        bool inGame = Assets.Scripts.Managers.EnemyManager.Instance != null;

        float winH = LineH + 10f;
        winH += LineH; if (_powerupOpen) winH += LineH * 4f;
        winH += LineH + 4f; // debug buttons row
        winH += LineH + 4f; // godmode + flymode row
        winH += LineH + 4f; // instakill + freeze row
        winH += LineH + 4f; // fly speed row
        winH += LineH + 4f; // BOMBUS button
        winH += LineH + 4f; // +1 min / -1 min buttons
        winH += LineH + 4f; // hitboxes toggle
        if (ShowHitboxes) winH += LineH + 4f; // hitbox distance slider
        winH += LineH + 8f;

        UiTheme.Backdrop(new Rect(wx, wy, WinW, winH), "ModMenu");
        GUI.Box(new Rect(wx, wy, WinW, winH), "MegaBonk Mod");

        float y = wy + LineH + 6f;
        var btnStyle    = GUI.skin.button;
        var toggleStyle = GUI.skin.toggle;

        // ── Powerups ──
        _powerupOpen = GUI.Toggle(new Rect(lx, y, cw, LineH), _powerupOpen,
            (_powerupOpen ? "[-] " : "[+] ") + "Spawn Powerup", btnStyle);
        y += LineH + 2f;
        if (_powerupOpen)
        {
            var pm = PickupManager.Instance;
            var pp = PlayerMovement.Instance;
            var pos = pp != null ? pp.transform.position : Vector3.zero;

            SpawnPowerupRow(pm, pos, lx, ref y, cw, EPickup.Health, "Health",   EPickup.Nuke,   "Nuke");
            SpawnPowerupRow(pm, pos, lx, ref y, cw, EPickup.Time,   "Clock",    EPickup.Shield, "Shield");
            SpawnPowerupRow(pm, pos, lx, ref y, cw, EPickup.Rage,   "Rage",     EPickup.Haste,  "Haste");
            SpawnPowerupRow(pm, pos, lx, ref y, cw, EPickup.Stonks, "Stonks",   EPickup.Magnet, "Magnet");
        }

        // ── God/Fly toggles ──
        float halfW = (cw - 4f) / 2f;
        GodMode = GUI.Toggle(new Rect(lx,              y, halfW, LineH), GodMode, GodMode ? "God Mode: ON"  : "God Mode: OFF",  btnStyle);
        FlyMode = GUI.Toggle(new Rect(lx + halfW + 4f, y, halfW, LineH), FlyMode, FlyMode ? "Fly Mode: ON"  : "Fly Mode: OFF",  btnStyle);
        if (GodMode || FlyMode) CheatsUsed = true;
        y += LineH + 4f;

        InstaKill     = GUI.Toggle(new Rect(lx,              y, halfW, LineH), InstaKill,     InstaKill     ? "Instakill: ON"      : "Instakill: OFF",      btnStyle);
        FreezeEnemies = GUI.Toggle(new Rect(lx + halfW + 4f, y, halfW, LineH), FreezeEnemies, FreezeEnemies ? "Freeze Enemies: ON" : "Freeze Enemies: OFF", btnStyle);
        if (InstaKill || FreezeEnemies) CheatsUsed = true;
        y += LineH + 4f;

        float labelW = 70f;
        GUI.Label(new Rect(lx, y + 3f, labelW, LineH), $"Fly Speed: {FlySpeed:F0}");
        FlySpeed = GUI.HorizontalSlider(new Rect(lx + labelW, y + 6f, cw - labelW, LineH - 4f), FlySpeed, 8f, 80f);
        y += LineH + 4f;

        // ── Debug buttons ──
        if (GUI.Button(new Rect(lx, y, halfW, LineH), "+1000 XP"))
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) { inv.AddXp(1000); CheatsUsed = true; }
        }
        if (GUI.Button(new Rect(lx + halfW + 4f, y, halfW, LineH), "+1000 Gold"))
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) { inv.ChangeGold(1000); CheatsUsed = true; }
        }
        y += LineH + 4f;

        GUI.enabled = inGame;
        if (GUI.Button(new Rect(lx, y, halfW, LineH), "+1 Min (Stage Timer)"))
        {
            StageTimerHelper.Advance(60f);
            CheatsUsed = true;
        }
        if (GUI.Button(new Rect(lx + halfW + 4f, y, halfW, LineH), "-1 Min (Stage Timer)"))
        {
            StageTimerHelper.Advance(-60f);
            CheatsUsed = true;
        }
        GUI.enabled = true;
        y += LineH + 4f;

        string bombusLabel = GiantBeeState.PhaseActive ? "Spawn BOMBUS (again)" : "Spawn BOMBUS";
        GUI.enabled = inGame;
        if (GUI.Button(new Rect(lx, y, cw, LineH), bombusLabel))
        {
            GiantBeeState.Spawn(manualOverride: true);
            CheatsUsed = true;
        }
        GUI.enabled = true;
        y += LineH + 4f;

        GUI.enabled = inGame;
        if (GUI.Button(new Rect(lx, y, cw, LineH), "Give 1 of Every Item"))
        {
            try
            {
                var inv = GameManager.Instance?.GetPlayerInventory();
                var dm = DataManager.Instance;
                if (inv?.itemInventory != null && dm?.unsortedItems != null)
                {
                    foreach (var id in dm.unsortedItems)
                    {
                        if (id != null)
                        {
                            try { inv.itemInventory.AddItem(id.eItem); } catch { }
                        }
                    }
                    CheatsUsed = true;
                }
            }
            catch { }
        }
        GUI.enabled = true;
        y += LineH + 4f;

        ShowHitboxes = GUI.Toggle(new Rect(lx, y, cw, LineH), ShowHitboxes,
            ShowHitboxes ? "Hitboxes: ON" : "Hitboxes: OFF", btnStyle);
        y += LineH + 4f;

        if (ShowHitboxes)
        {
            float distLabelW = 100f;
            GUI.Label(new Rect(lx, y + 3f, distLabelW, LineH), $"HB Dist: {HitboxMaxDist:F0}");
            HitboxMaxDist = GUI.HorizontalSlider(new Rect(lx + distLabelW, y + 6f, cw - distLabelW, LineH - 4f), HitboxMaxDist, 100f, 1000f);
            y += LineH + 4f;
        }

        GUI.Label(new Rect(lx, y + 4f, cw, LineH), "F1 toggle | F2 chart | drag title | drag ↘ to resize");

        _lastWinH = winH;
        _frame.End(savedMatrix);
        _frame.DrawGrip(WinW, winH);
    }

    internal static void ResetVersionScan()
    {
    }

    private void OnRenderObject()
    {
        if (!ShowHitboxes) return;
        if (Camera.current != Camera.main) return;

        if (_hitboxMat == null)
        {
            var shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            _hitboxMat = new Material(shader);
            _hitboxMat.hideFlags = HideFlags.HideAndDontSave;
            _hitboxMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _hitboxMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _hitboxMat.SetInt("_Cull",     (int)UnityEngine.Rendering.CullMode.Off);
            _hitboxMat.SetInt("_ZWrite", 0);
            _hitboxMat.SetInt("_ZTest",  (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        // Refresh collider cache every 30 frames
        if (Time.frameCount - _colliderCacheFrame > 30)
        {
            _cachedColliders    = UnityEngine.Object.FindObjectsOfType<Collider>();
            _colliderCacheFrame = Time.frameCount;
        }
        if (_cachedColliders == null) return;

        var cam      = Camera.main;
        var camPos   = cam.transform.position;
        float maxSq  = HitboxMaxDist * HitboxMaxDist;

        _hitboxMat.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);

        try
        {
            foreach (var col in _cachedColliders)
            {
                if (col == null || !col.enabled) continue;
                var bounds = col.bounds;
                if ((bounds.center - camPos).sqrMagnitude > maxSq) continue;

                GL.Color(col.isTrigger ? new Color(0f, 0.8f, 1f, 0.9f) : new Color(0f, 1f, 0f, 0.9f));

                var box     = col.TryCast<BoxCollider>();
                if (box     != null) { HbDrawBox(box);       continue; }
                var sphere  = col.TryCast<SphereCollider>();
                if (sphere  != null) { HbDrawSphere(sphere); continue; }
                var capsule = col.TryCast<CapsuleCollider>();
                if (capsule != null) { HbDrawCapsule(capsule); continue; }
            }
        }
        catch { }

        GL.End();
        GL.PopMatrix();
    }

    private static void HbDrawBox(BoxCollider box)
    {
        var t = box.transform;
        var c = box.center;
        var e = box.size * 0.5f;
        var p000 = t.TransformPoint(c + new Vector3(-e.x, -e.y, -e.z));
        var p100 = t.TransformPoint(c + new Vector3( e.x, -e.y, -e.z));
        var p110 = t.TransformPoint(c + new Vector3( e.x, -e.y,  e.z));
        var p010 = t.TransformPoint(c + new Vector3(-e.x, -e.y,  e.z));
        var p001 = t.TransformPoint(c + new Vector3(-e.x,  e.y, -e.z));
        var p101 = t.TransformPoint(c + new Vector3( e.x,  e.y, -e.z));
        var p111 = t.TransformPoint(c + new Vector3( e.x,  e.y,  e.z));
        var p011 = t.TransformPoint(c + new Vector3(-e.x,  e.y,  e.z));
        HbLine(p000,p100); HbLine(p100,p110); HbLine(p110,p010); HbLine(p010,p000);
        HbLine(p001,p101); HbLine(p101,p111); HbLine(p111,p011); HbLine(p011,p001);
        HbLine(p000,p001); HbLine(p100,p101); HbLine(p110,p111); HbLine(p010,p011);
    }

    private static void HbDrawSphere(SphereCollider sphere)
    {
        var t = sphere.transform;
        var s = t.lossyScale;
        float r = sphere.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        var wc = t.TransformPoint(sphere.center);
        HbCircle(wc, Vector3.right,   Vector3.up,      r);
        HbCircle(wc, Vector3.right,   Vector3.forward, r);
        HbCircle(wc, Vector3.up,      Vector3.forward, r);
    }

    private static void HbDrawCapsule(CapsuleCollider cap)
    {
        var t = cap.transform;
        var s = t.lossyScale;

        // Pick axis/perp vectors and scale factors based on capsule direction
        Vector3 axis, a, b;
        float scaleAlong, scalePerp;
        switch (cap.direction)
        {
            case 0: // X
                axis = t.right; a = t.up; b = t.forward;
                scaleAlong = Mathf.Abs(s.x);
                scalePerp  = Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z));
                break;
            case 2: // Z
                axis = t.forward; a = t.right; b = t.up;
                scaleAlong = Mathf.Abs(s.z);
                scalePerp  = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
                break;
            default: // Y (1)
                axis = t.up; a = t.right; b = t.forward;
                scaleAlong = Mathf.Abs(s.y);
                scalePerp  = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
                break;
        }

        float r     = cap.radius * scalePerp;
        float halfH = Mathf.Max(0f, cap.height * 0.5f * scaleAlong - r);
        var wc  = t.TransformPoint(cap.center);
        var top = wc + axis * halfH;
        var bot = wc - axis * halfH;
        HbCircle(top, a, b, r);
        HbCircle(bot, a, b, r);
        HbLine(top + a * r, bot + a * r); HbLine(top - a * r, bot - a * r);
        HbLine(top + b * r, bot + b * r); HbLine(top - b * r, bot - b * r);
    }

    private static void HbCircle(Vector3 center, Vector3 a, Vector3 b, float r, int seg = 16)
    {
        float step = 2f * Mathf.PI / seg;
        var prev = center + a * r;
        for (int i = 1; i <= seg; i++)
        {
            float angle = i * step;
            var next = center + (a * Mathf.Cos(angle) + b * Mathf.Sin(angle)) * r;
            HbLine(prev, next);
            prev = next;
        }
    }

    private static void HbLine(Vector3 a, Vector3 b) { GL.Vertex(a); GL.Vertex(b); }

    private static void SpawnPowerupRow(
        PickupManager pm, Vector3 pos,
        float x, ref float y, float cw,
        EPickup left, string leftLabel, EPickup right, string rightLabel)
    {
        float halfW = (cw - 4f) / 2f;
        if (GUI.Button(new Rect(x, y, halfW, LineH), leftLabel)  && pm != null)
            pm.SpawnPickup(left,  pos, 1, false, 0f);
        if (GUI.Button(new Rect(x + halfW + 4f, y, halfW, LineH), rightLabel) && pm != null)
            pm.SpawnPickup(right, pos, 1, false, 0f);
        y += LineH;
    }

}
