using UnityEngine;
using TMPro;
using Assets.Scripts.Inventory__Items__Pickups.Pickups;

namespace MegaBonkMod;

public class ModGui : MonoBehaviour
{
    public ModGui(System.IntPtr ptr) : base(ptr) { }

    internal static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> MainThread = new();

    private bool _visible;

    // damage chart
    internal static bool ChartDisabled;
    internal static bool NeedVersionPatch;
    private GameObject _statsParent;
    private GameObject _damageWindow;
    private GameObject _statsWindowObj;
    private GameObject _questsWindow;

    private Vector2 _winPos = new(20f, 20f);
    private bool _dragging;
    private Vector2 _dragOffset;

    private bool _powerupOpen  = true;

    private const float FastFallSpeed    = 15f;
    private const float FastFallRampTime = 0.2f;
    private       float _fastFallTimer   = 0f;

    private const float WinW  = 440f;
    private const float PadX  = 8f;
    private const float LineH = 22f;

    private void Update()
    {
        while (MainThread.TryDequeue(out var action))
            try { action(); } catch { }

        if (NeedVersionPatch)
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

        if (Input.GetKeyDown(KeyCode.F1))
            _visible = !_visible;

        if (Input.GetKeyDown(KeyCode.F2) && !ChartDisabled)
            ToggleDamageChart();

        try
        {
            var pm = PlayerMovement.Instance;
            if (pm != null)
            {
                unsafe
                {
                    bool onGround = *(byte*)(pm.Pointer + 0xDF) != 0;
                    bool active  = !onGround && MyInputManager.GetButton(MyInputManager.Slide);

                    if (active)
                    {
                        _fastFallTimer = Mathf.Min(_fastFallTimer + Time.deltaTime, FastFallRampTime);
                        float t = _fastFallTimer / FastFallRampTime;
                        float speed = FastFallSpeed * (t * t);
                        if (speed > 0.01f)
                        {
                            var rb = pm.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                var v = rb.velocity;
                                if (v.y <= 0f)
                                    rb.velocity = new Vector3(v.x, Mathf.Min(v.y, -speed), v.z);
                            }
                        }
                    }
                    else
                    {
                        _fastFallTimer = 0f;
                    }
                }
            }
        }
        catch { }

        if (!_visible) return;

        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        var mp = new Vector2(mx, my);

        if (Input.GetMouseButtonDown(0))
        {
            var titleBar = new Rect(_winPos.x, _winPos.y, WinW, LineH + 4f);
            if (titleBar.Contains(mp))
            {
                _dragging = true;
                _dragOffset = mp - _winPos;
            }
        }
        if (Input.GetMouseButtonUp(0))
            _dragging = false;
        if (_dragging && Input.GetMouseButton(0))
            _winPos = mp - _dragOffset;
    }

    internal void ResetChartCache() { _statsParent = null; }

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

                var rt = _statsParent.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin        = new Vector2(0f, 0.5f);
                    rt.anchorMax        = new Vector2(0f, 0.5f);
                    rt.pivot            = new Vector2(0f, 0.5f);
                    rt.anchoredPosition = new Vector2(20f, 0f);
                }
            }
        }
        catch { }
    }

    private void OnGUI()
    {
        if (!_visible) return;

        float wx = _winPos.x;
        float wy = _winPos.y;
        float cw = WinW - PadX * 2f;
        float lx = wx + PadX;
        float indentX = lx + 12f;
        float indentW = cw - 12f;

        float winH = LineH + 10f;
        winH += LineH; if (_powerupOpen) winH += LineH * 4f;
        winH += LineH + 4f; // debug buttons row
        winH += LineH + 8f;

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

        // ── Debug buttons ──
        float halfW = (cw - 4f) / 2f;
        if (GUI.Button(new Rect(lx, y, halfW, LineH), "+1000 XP"))
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) inv.AddXp(1000);
        }
        if (GUI.Button(new Rect(lx + halfW + 4f, y, halfW, LineH), "+1000 Gold"))
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            if (inv != null) inv.ChangeGold(1000);
        }
        y += LineH + 4f;

        GUI.Label(new Rect(lx, y + 4f, cw, LineH), "F1 to toggle  |  F2 dmg chart  |  Drag title bar");
    }

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
