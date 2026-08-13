using BepInEx.Configuration;
using UnityEngine;
using Assets.Scripts.Utility;
using Assets.Scripts.Inventory__Items__Pickups.Stats;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// POWERUP TRACKER  (hotkey: Hotkeys.PowerupTracker, default .)
//
// Shows active status effects with their remaining duration. Reads from
// PlayerInventory.statusEffects.statusEffects (Dictionary<EStatusEffect, StatusEffect>).
// Remaining time = expirationTime - (MyTime.time - addedTime).
// ─────────────────────────────────────────────────────────────────────────
internal static class PowerupTracker
{
    internal static bool Enabled;

    static readonly GuiWindowFrame _frame = new(new Vector2(10f, 300f));
    const float WinW = 170f, PadX = 10f, LineH = 20f;

    static float WinHeight(int count) =>
        LineH + 4f + Mathf.Max(count, 1) * LineH + 4f;

    internal static void Init(ConfigFile cfg) { _frame.Init(cfg, "PowerupTracker"); }

    internal static void Toggle() => Enabled = !Enabled;

    static int _cachedCount;

    internal static void HandleInput()
    {
        if (Enabled)
            _frame.HandleInput(WinW, WinHeight(_cachedCount), LineH + 4f);
    }

    struct EffectInfo
    {
        public EStatusEffect Type;
        public float Remaining;
        public float Duration;
    }

    static EffectInfo[] _buf = System.Array.Empty<EffectInfo>();

    static EffectInfo[] GetActiveEffects()
    {
        try
        {
            var inv = GameManager.Instance?.GetPlayerInventory();
            var pse = inv?.statusEffects;
            var dict = pse?.statusEffects;
            if (dict == null || dict.Count == 0) { _buf = System.Array.Empty<EffectInfo>(); _cachedCount = 0; return _buf; }

            float now = MyTime.time;
            int n = 0;
            if (_buf.Length < dict.Count) _buf = new EffectInfo[dict.Count];
            foreach (var kv in dict)
            {
                var se = kv.Value;
                if (se == null) continue;
                float remaining = se.expirationTime - now;
                if (remaining <= 0f) continue;
                _buf[n++] = new EffectInfo
                {
                    Type      = kv.Key,
                    Remaining = remaining,
                    Duration  = se.expirationTime,
                };
            }
            _cachedCount = n;
            return _buf;
        }
        catch { _buf = System.Array.Empty<EffectInfo>(); _cachedCount = 0; return _buf; }
    }

    static string EffectName(EStatusEffect e) => e switch
    {
        EStatusEffect.Haste          => "Haste",
        EStatusEffect.Rage           => "Rage",
        EStatusEffect.Shield         => "Shield",
        EStatusEffect.Stonks         => "Stonks",
        EStatusEffect.TimeFreeze     => "Time Freeze",
        EStatusEffect.Invulnerability => "Invulnerability",
        EStatusEffect.Slow           => "Slow",
        EStatusEffect.Freeze         => "Freeze",
        EStatusEffect.Bleed          => "Bleed",
        EStatusEffect.Poison         => "Poison",
        EStatusEffect.BossPoison     => "Boss Poison",
        _                            => e.ToString(),
    };

    static Color EffectColor(EStatusEffect e) => e switch
    {
        EStatusEffect.Rage            => new Color(1f, 0.3f, 0.3f),
        EStatusEffect.TimeFreeze      => new Color(1f, 1f, 1f),
        EStatusEffect.Haste           => new Color(0.4f, 0.8f, 1f),
        EStatusEffect.Shield          => new Color(0.3f, 1f, 0.4f),
        EStatusEffect.Stonks          => new Color(1f, 0.8f, 0.2f),
        _                             => new Color(1f, 1f, 1f),
    };

    static readonly System.Text.StringBuilder _sb = new(64);

    internal static void Draw()
    {
        if (!Enabled) return;

        var effects = GetActiveEffects();
        float winH = WinHeight(_cachedCount);
        var saved = _frame.Begin();
        float ox = _frame.Pivot.x, oy = _frame.Pivot.y;
        UiTheme.Backdrop(new Rect(ox, oy, WinW, winH));
        GUI.Box(new Rect(ox, oy, WinW, winH), "Active Powerups");

        float cw = WinW - PadX * 2f;
        float lx = ox + PadX;
        float y  = oy + LineH + 4f;

        if (_cachedCount == 0)
        {
            GUI.Label(new Rect(lx, y, cw, LineH), "No active effects");
        }
        else
        {
            for (int i = 0; i < _cachedCount; i++)
            {
                var fx = effects[i];
                _sb.Clear();
                _sb.Append(EffectName(fx.Type));
                _sb.Append(": ");
                _sb.Append(fx.Remaining.ToString("F1"));
                _sb.Append('s');
                var prevCol = GUI.color;
                GUI.color = EffectColor(fx.Type);
                GUI.Label(new Rect(lx, y, cw, LineH), _sb.ToString());
                GUI.color = prevCol;
                y += LineH;
            }
        }

        _frame.End(saved);
        _frame.DrawGrip(WinW, winH);
    }
}
