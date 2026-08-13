using BepInEx.Configuration;
using UnityEngine;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Inventory.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Items;
using Assets.Scripts.Managers;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// CHEST ODDS TRACKER  (hotkey: Hotkeys.ChestOddsTracker, default F8)
//
// Shows the rarity drop-rate percentages for chests based on the player's
// current Luck stat. Uses the game's formula: S = ln(Luck/100+1)*1.5,
// then W_i = baseWeight_i * 1.5^(-(3-i)*S), normalized to percentages.
//
// Press the [H]/[V] button to toggle horizontal (single-line) vs vertical
// layout. The choice persists across sessions.
// ─────────────────────────────────────────────────────────────────────────
internal static class ChestOddsTracker
{
    internal static bool Visible;

    static readonly GuiWindowFrame _frame = new(new Vector2(10f, 500f));
    const float PadX = 10f, LineH = 20f;
    const float BaseW_V = 190f, BaseH_V = 156f;
    const float BaseW_H = 420f, BaseH_H = 56f;

    static bool _horizontal;
    static ConfigEntry<bool> _cfgHoriz;

    static float WinW => _horizontal ? BaseW_H : BaseW_V;
    static float WinH => _horizontal ? BaseH_H : BaseH_V;

    internal static void Init(ConfigFile cfg)
    {
        _frame.Init(cfg, "ChestOddsTracker");
        _cfgHoriz = cfg.Bind("ChestOddsTracker", "Horizontal", false,
            "Show chest odds in a single horizontal line.");
        _horizontal = _cfgHoriz.Value;
    }

    internal static void Toggle() => Visible = !Visible;

    internal static void HandleInput()
    {
        if (Visible)
            _frame.HandleInput(WinW, WinH, LineH + 4f);
    }

    static readonly string[] TierNames = { "Common", "Rare", "Epic", "Legendary" };
    static readonly string[] TierAbbr  = { "C", "R", "E", "L" };
    static readonly float[] BaseWeights = { 70f, 15f, 6f, 1.5f };
    static readonly float A = 1.5f;

    static Color RarityColor(int i) => i switch
    {
        0 => new Color(1f, 1f, 1f),
        1 => new Color(0.4f, 0.8f, 1f),
        2 => new Color(0.7f, 0.4f, 1f),
        3 => new Color(1f, 0.85f, 0.2f),
        _ => new Color(1f, 1f, 1f),
    };

    static float[] CalculateWeights(float luck)
    {
        float s = Mathf.Log(luck + 1f) * 1.5f;
        float[] w = new float[4];
        for (int i = 0; i < 4; i++)
            w[i] = BaseWeights[i] * Mathf.Pow(A, -(3f - i) * s);
        return w;
    }

    static float[] _cachedWeights;
    static float _cachedLuck = -1f;
    static int _cachedKeys = -1;
    static readonly System.Text.StringBuilder _sb = new(64);

    internal static void Draw()
    {
        if (!Visible) return;

        float winW = WinW, winH = WinH;
        var saved = _frame.Begin();
        float ox = _frame.Pivot.x, oy = _frame.Pivot.y;
        UiTheme.Backdrop(new Rect(ox, oy, winW, winH));
        GUI.Box(new Rect(ox, oy, winW, winH), "Chest Rarity Odds");

        float cw = winW - PadX * 2f;
        float lx = ox + PadX;
        float y  = oy + LineH + 4f;

        // mode toggle button (inside scaled context so it resizes with the window)
        float btnW = 24f;
        string btnLabel = _horizontal ? "V" : "H";
        if (GUI.Button(new Rect(ox + winW - btnW - 4f, oy + 2f, btnW, LineH - 2f), btnLabel))
        {
            _horizontal = !_horizontal;
            _cfgHoriz.Value = _horizontal;
        }

        try
        {
            float luck = PlayerStats.GetStat(EStat.Luck);
            float luckPct = luck * 100f;

            // Key item free chest chance (15% per stack, hyperbolic: n/(n+10))
            int keyCount = 0;
            try
            {
                var inv = GameManager.Instance?.GetPlayerInventory()?.itemInventory;
                if (inv != null) keyCount = inv.GetAmount(EItem.Key);
            }
            catch { }
            float freeChest = keyCount > 0
                ? Assets.Scripts.Inventory__Items__Pickups.Stats.StatScaling.HyperbolicScaling(
                    0.20f * keyCount, 1f, 1f)
                : 0f;

            if (luck != _cachedLuck || keyCount != _cachedKeys)
            {
                _cachedLuck = luck;
                _cachedKeys = keyCount;
                _cachedWeights = CalculateWeights(luck);
            }

            var weights = _cachedWeights;
            if (weights != null)
            {
                float total = 0f;
                for (int i = 0; i < weights.Length; i++) total += weights[i];

                if (total > 0f)
                {
                    if (_horizontal)
                    {
                        // Build segments, measure each, draw sequentially with colors.
                        string prefix = "Luck: " + luckPct.ToString("F0") + "%  Free: " + (freeChest * 100f).ToString("F0") + "%  ";
                        string[] segs = new string[4];
                        for (int i = 0; i < 4; i++)
                        {
                            float pct = weights[i] / total * 100f;
                            segs[i] = TierAbbr[i] + ": " + pct.ToString("F1") + "%  ";
                        }

                        var style = GUI.skin.label;
                        float cx = lx;

                        // Draw prefix in white
                        var sz = style.CalcSize(new GUIContent(prefix));
                        GUI.Label(new Rect(cx, y, sz.x, LineH), prefix);
                        cx += sz.x;

                        // Draw each rarity segment in its color
                        for (int i = 0; i < 4; i++)
                        {
                            sz = style.CalcSize(new GUIContent(segs[i]));
                            var prevCol = GUI.color;
                            GUI.color = RarityColor(i);
                            GUI.Label(new Rect(cx, y, sz.x, LineH), segs[i]);
                            GUI.color = prevCol;
                            cx += sz.x;
                        }
                        y += LineH;
                    }
                    else
                    {
                        // Vertical: Luck + Free Chest on their own lines, then each rarity
                        _sb.Clear(); _sb.Append("Luck: "); _sb.Append(luckPct.ToString("F0")); _sb.Append('%');
                        GUI.Label(new Rect(lx, y, cw, LineH), _sb.ToString());
                        y += LineH;

                        _sb.Clear(); _sb.Append("Free Chest: "); _sb.Append((freeChest * 100f).ToString("F0")); _sb.Append('%');
                        GUI.Label(new Rect(lx, y, cw, LineH), _sb.ToString());
                        y += LineH;

                        for (int i = 0; i < weights.Length; i++)
                        {
                            float pct = weights[i] / total * 100f;
                            _sb.Clear();
                            _sb.Append(TierNames[i]);
                            int pad = 12 - TierNames[i].Length;
                            while (pad-- > 0) _sb.Append(' ');
                            _sb.Append(' ');
                            _sb.Append(pct.ToString("F1"));
                            _sb.Append('%');
                            var prevCol = GUI.color;
                            GUI.color = RarityColor(i);
                            GUI.Label(new Rect(lx, y, cw, LineH), _sb.ToString());
                            GUI.color = prevCol;
                            y += LineH;
                        }
                    }
                }
                else
                {
                    GUI.Label(new Rect(lx, y, cw, LineH), "No data available");
                    y += LineH;
                }
            }
            else
            {
                GUI.Label(new Rect(lx, y, cw, LineH), "No data available");
                y += LineH;
            }
        }
        catch
        {
            GUI.Label(new Rect(lx, y, cw, LineH), "Start a run first.");
            y += LineH;
        }

        _frame.End(saved);
        _frame.DrawGrip(winW, winH);
    }
}
