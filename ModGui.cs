using UnityEngine;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;

namespace MegaBonkMod;

public class ModGui : MonoBehaviour
{
    public ModGui(System.IntPtr ptr) : base(ptr) { }

    internal static readonly System.Collections.Concurrent.ConcurrentQueue<System.Action> MainThread = new();

    private bool _visible;

    private Vector2 _winPos = new(20f, 20f);
    private bool _dragging;
    private Vector2 _dragOffset;


    private bool _chaosOpen   = true;
    private bool _shrineOpen  = true;
    private bool _itemCapOpen = true;
    private bool _sizeCapOpen = true;

    private float _chaosScrollY;
    private float _shrineScrollY;
    private Rect  _chaosScrollRect;
    private Rect  _shrineScrollRect;

    private const float FastFallSpeed    = 20f;
    private const float FastFallRampTime = 1f;
    private       float _fastFallTimer   = 0f;

    private const float WinW   = 440f;
    private const float PadX   = 8f;
    private const float LineH  = 22f;
    private const float ScrollH = 180f;

    private static readonly (int value, string label)[] StatLabels =
    {
        (0,  "Max Health"),
        (1,  "Health Regen"),
        (2,  "Shield"),
        (3,  "Thorns"),
        (4,  "Armor"),
        (5,  "Evasion"),
        (6,  "Evolve"),
        (7,  "Damage Reduction Multiplier"),
        (8,  "Damage Cooldown Multiplier"),
        (9,  "Size Multiplier"),
        (10, "Duration Multiplier"),
        (11, "Projectile Speed Multiplier"),
        (12, "Damage Multiplier"),
        (13, "Unused0"),
        (14, "Effect Duration Multiplier"),
        (15, "Attack Speed"),
        (16, "Projectiles"),
        (17, "Lifesteal"),
        (18, "Crit Chance"),
        (19, "Crit Damage"),
        (20, "Fire Damage"),
        (21, "Ice Damage"),
        (22, "Lightning Damage"),
        (23, "Elite Damage Multiplier"),
        (24, "Knockback Multiplier"),
        (25, "Move Speed Multiplier"),
        (26, "Jump Height"),
        (27, "Fall Damage Reduction"),
        (28, "Slam"),
        (29, "Pickup Range"),
        (30, "Luck"),
        (31, "Gold Increase Multiplier"),
        (32, "XP Increase Multiplier"),
        (33, "Chest Increase Multiplier"),
        (34, "Chest Price Multiplier"),
        (35, "Shop Price Reduction"),
        (36, "Holiness"),
        (37, "Wickedness"),
        (38, "Difficulty"),
        (39, "Elite Spawn Increase"),
        (40, "Powerup Boost Multiplier"),
        (41, "Powerup Chance"),
        (42, "Burn Chance"),
        (43, "Freeze Chance"),
        (44, "Weapon Burst Cooldown"),
        (45, "Projectile Bounces"),
        (46, "Extra Jumps"),
        (47, "Overheal"),
        (48, "Healing Multiplier"),
        (49, "Silver Increase Multiplier"),
        (50, "Enemy Amount Multiplier"),
        (51, "Enemy Size Multiplier"),
        (52, "Enemy Speed Multiplier"),
        (53, "Enemy HP Multiplier"),
        (54, "Enemy Damage Multiplier"),
        (55, "Enemy Scaling Multiplier"),
        (56, "Poison Damage Multiplier"),
    };

    private void Update()
    {
        while (MainThread.TryDequeue(out var action))
            try { action(); } catch { }

        if (Input.GetKeyDown(KeyCode.F1))
            _visible = !_visible;

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
                        float speed = Mathf.Lerp(0f, FastFallSpeed, _fastFallTimer / FastFallRampTime);
                        if (speed > 0.01f)
                        {
                            var rb = pm.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                var v = rb.velocity;
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

        float scrollDelta = Input.mouseScrollDelta.y;
        if (scrollDelta != 0f)
        {
            if (_chaosOpen  && _chaosScrollRect.Contains(mp))
                _chaosScrollY  = Mathf.Clamp(_chaosScrollY  - scrollDelta * 30f, 0f, MaxScroll(StatLabels));
            else if (_shrineOpen && _shrineScrollRect.Contains(mp))
                _shrineScrollY = Mathf.Clamp(_shrineScrollY - scrollDelta * 30f, 0f, MaxScroll(StatLabels));
        }
    }

    private static float MaxScroll((int, string)[] labels) =>
        Mathf.Max(0f, labels.Length * LineH - ScrollH);

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
        winH += LineH; if (_chaosOpen)   winH += ScrollH + 4f;
        winH += LineH; if (_shrineOpen)  winH += ScrollH + 4f;
        winH += LineH; if (_itemCapOpen) winH += LineH * 4f;
        winH += LineH; if (_sizeCapOpen) winH += LineH * 2f;
        winH += LineH + 4f; // debug buttons row
        winH += LineH + 8f;

        GUI.Box(new Rect(wx, wy, WinW, winH), "MegaBonk Mod");

        float y = wy + LineH + 6f;
        var btnStyle    = GUI.skin.button;
        var toggleStyle = GUI.skin.toggle;

        // ── Chaos / Gamble Tome / Dicehead ──
        _chaosOpen = GUI.Toggle(new Rect(lx, y, cw, LineH), _chaosOpen,
            (_chaosOpen ? "[-] " : "[+] ") + "Stat Blacklist - Chaos / Gamble Tome / Dicehead", btnStyle);
        y += LineH + 2f;
        if (_chaosOpen)
        {
            _chaosScrollRect = new Rect(indentX, y, indentW, ScrollH);
            GUI.BeginGroup(_chaosScrollRect, GUIContent.none, GUIStyle.none);
            DrawStatToggles(Plugin.BlacklistedStats, Plugin.FullStatPool,
                EncounterUtility.upgradableStatsChaosAndGamble, _chaosScrollY, indentW, toggleStyle);
            GUI.EndGroup();
            y += ScrollH + 4f;
        }

        // ── Charge Shrines ──
        _shrineOpen = GUI.Toggle(new Rect(lx, y, cw, LineH), _shrineOpen,
            (_shrineOpen ? "[-] " : "[+] ") + "Stat Blacklist - Charge Shrines", btnStyle);
        y += LineH + 2f;
        if (_shrineOpen)
        {
            _shrineScrollRect = new Rect(indentX, y, indentW, ScrollH);
            GUI.BeginGroup(_shrineScrollRect, GUIContent.none, GUIStyle.none);
            DrawStatToggles(Plugin.BlacklistedShrineStats, Plugin.FullShrineStatPool,
                EncounterUtility.upgradableStatsShrines, _shrineScrollY, indentW, toggleStyle);
            GUI.EndGroup();
            y += ScrollH + 4f;
        }

        // ── Item Cap Removal ──
        _itemCapOpen = GUI.Toggle(new Rect(lx, y, cw, LineH), _itemCapOpen,
            (_itemCapOpen ? "[-] " : "[+] ") + "Item Caps", btnStyle);
        y += LineH + 2f;
        if (_itemCapOpen)
        {
            ItemToggle(EItem.Anvil,           "Anvil (cap 2)",             indentX, ref y, indentW, toggleStyle);
            ItemToggle(EItem.OverpoweredLamp, "Overpowered Lamp (cap 3)", indentX, ref y, indentW, toggleStyle);
            ItemToggle(EItem.ZaWarudo,        "Za Warudo (cap 10)",        indentX, ref y, indentW, toggleStyle);
        }

        // ── Size Cap Removal ──
        _sizeCapOpen = GUI.Toggle(new Rect(lx, y, cw, LineH), _sizeCapOpen,
            (_sizeCapOpen ? "[-] " : "[+] ") + "Size Cap Removal", btnStyle);
        y += LineH + 2f;
        if (_sizeCapOpen)
        {
            bool gt = GUI.Toggle(new Rect(indentX, y, indentW, LineH), Plugin.PatchGrandmasTonic,
                new GUIContent("Grandma's Secret Tonic"), toggleStyle);
            if (gt != Plugin.PatchGrandmasTonic) Plugin.PatchGrandmasTonic = gt;
            y += LineH;

            bool sm = GUI.Toggle(new Rect(indentX, y, indentW, LineH), Plugin.PatchSpicyMeatball,
                new GUIContent("Spicy Meatball"), toggleStyle);
            if (sm != Plugin.PatchSpicyMeatball) Plugin.PatchSpicyMeatball = sm;
            y += LineH;
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

        GUI.Label(new Rect(lx, y + 4f, cw, LineH), "F1 to toggle  |  Drag title bar  |  Scroll in lists");
    }

    private static void DrawStatToggles(
        System.Collections.Generic.HashSet<int> blacklist,
        System.Collections.Generic.List<int> fullPool,
        Il2CppSystem.Collections.Generic.List<EStat> livePool,
        float scrollY,
        float width,
        GUIStyle style)
    {
        var displayList = fullPool.Count > 0 ? fullPool : null;
        float itemY = -scrollY;

        foreach (var (statInt, label) in StatLabels)
        {
            bool wasInPool = displayList == null || displayList.Contains(statInt);
            if (!wasInPool && !blacklist.Contains(statInt)) continue;

            bool isEnabled = !blacklist.Contains(statInt);
            bool nowEnabled = GUI.Toggle(new Rect(0f, itemY, width, LineH), isEnabled, new GUIContent(label), style);

            if (nowEnabled != isEnabled)
            {
                var eStat = (EStat)statInt;
                if (!nowEnabled)
                {
                    blacklist.Add(statInt);
                    if (livePool != null && livePool.Contains(eStat)) livePool.Remove(eStat);
                }
                else
                {
                    blacklist.Remove(statInt);
                    if (livePool != null && !livePool.Contains(eStat)) livePool.Add(eStat);
                }
                Plugin.SaveConfig();
            }
            itemY += LineH;
        }
    }

    private static void ItemToggle(EItem item, string label, float x, ref float y, float w, GUIStyle style)
    {
        bool cur = Plugin.ActiveUncappedItems.Contains(item);
        bool now = GUI.Toggle(new Rect(x, y, w, LineH), cur, new GUIContent(label), style);
        if (now != cur)
        {
            if (now) Plugin.ActiveUncappedItems.Add(item);
            else Plugin.ActiveUncappedItems.Remove(item);
            Plugin.SaveConfig();
        }
        y += LineH;
    }
}
