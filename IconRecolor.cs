#nullable disable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Utility;                       // MyColorUtility
using Assets.Scripts.Inventory__Items__Pickups.Items; // ItemData, EItem, EItemRarity

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// ICON RIM RECOLOR
//
// Each item icon ships with its rarity-colored outline BAKED into the texture
// pixels (there is no separate UI element for it — UnlockContainer only has
// icon + two background RawImages, and the square frame is tinted at runtime).
// When we move an item to a new rarity, the square frame recolors itself but
// the baked rim stays the old rarity color.
//
// We can't edit the read-only asset, but we CAN swap the RawImage's displayed
// texture: blit the source into a temp RenderTexture, read it back into a
// readable Texture2D, remap pixels close to the OLD rarity color → the NEW
// rarity color (alpha preserved), and show that copy instead. Built once per
// item and cached.
// ─────────────────────────────────────────────────────────────────────────
internal static class IconRecolor
{
    // eItem → original rarity, captured in DataManager.Load BEFORE we mutate.
    internal static readonly Dictionary<EItem, EItemRarity> Original = new();

    // eItem → recolored texture (null = build failed, don't retry).
    static readonly Dictionary<EItem, Texture2D> _cache = new();

    // Euclidean match tolerance (0-255 space) around the auto-detected rim color.
    // Widened from 95: the remap is already spatially gated to the outline band
    // (RimBand px from transparency), so a looser color match just catches the
    // anti-aliased rim shades that were being left behind — smoother edges,
    // without bleeding into interior art (e.g. Slurp Gloves' body).
    const int Tol = 130;

    // Recolor the item's baked rim AT THE SOURCE: swap the Texture stored on
    // ItemData.icon. Every UI surface (unlocks menu, in-run inventory, Shady Guy
    // shop, level-up cards, world-drop/HUD icon) pulls its icon from GetIcon →
    // this field, so one swap fixes them all. Called once per item right after
    // we mutate rarities in DataManager.Load. Idempotent + cached.
    internal static void RecolorSource(ItemData id)
    {
        if (id == null) return;

        var eItem = id.eItem;
        if (!Original.TryGetValue(eItem, out var oldR)) return;
        var newR = id.rarity;
        if (oldR == newR) return;                       // rarity unchanged → nothing to do

        if (_cache.TryGetValue(eItem, out var cached))
        {
            if (cached != null) id.icon = cached;        // already built → reuse
            return;
        }

        var src = id.icon;
        if (src == null) return;

        try
        {
            var tex = Build(src, MyColorUtility.GetItemRarityColor(newR));
            _cache[eItem] = tex;
            if (tex != null) id.icon = tex;
        }
        catch (System.Exception e)
        {
            _cache[eItem] = null;                        // cache failure → no retry storm
            Plugin.Log.LogWarning($"[IconRecolor] {eItem}: {e.Message}");
        }
    }

    static Texture2D Build(Texture src, Color newC)
    {
        int w = src.width, h = src.height;

        var rt   = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        var px = tex.GetPixels32();
        const int RimBand = 2;

        // 0) Flood-fill the "outside" (exterior transparency) from the texture
        //    border inward through connected transparent pixels. Transparent
        //    pixels NOT reached are INTERIOR holes — gaps inside the art (e.g. the
        //    spaces between Slurp Gloves' straps). We must treat only the outer
        //    boundary as the rim; otherwise art bordering an interior hole gets
        //    wrongly tinted (the blue blobs in the middle of the gloves).
        var outside = new bool[w * h];
        var stack   = new System.Collections.Generic.Stack<int>();
        void Seed(int idx) { if (px[idx].a < 32 && !outside[idx]) { outside[idx] = true; stack.Push(idx); } }
        for (int x = 0; x < w; x++) { Seed(x); Seed((h - 1) * w + x); }
        for (int y = 0; y < h; y++) { Seed(y * w); Seed(y * w + (w - 1)); }
        while (stack.Count > 0)
        {
            int i = stack.Pop(); int x = i % w, y = i / w;
            if (x > 0)     Seed(i - 1);
            if (x < w - 1) Seed(i + 1);
            if (y > 0)     Seed(i - w);
            if (y < h - 1) Seed(i + w);
        }

        // True if (x,y) is within RimBand of an EXTERIOR pixel or the texture
        // border — i.e. it sits on the outer outline (never an interior hole).
        bool NearOutside(int x, int y)
        {
            for (int oy = -RimBand; oy <= RimBand; oy++)
                for (int ox = -RimBand; ox <= RimBand; ox++)
                {
                    int nx = x + ox, ny = y + oy;
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) return true;
                    if (outside[ny * w + nx]) return true;
                }
            return false;
        }

        // 1) Auto-detect the rim color: opaque, saturated pixels that border the
        //    EXTERIOR (not an interior hole). Averaged → actual rim shade. Adapts
        //    per-icon, so it works even when the baked rim differs from the game's
        //    data rarity color.
        long sr = 0, sg = 0, sb = 0; int n = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                var p = px[i];
                if (p.a < 32) continue;

                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                            || outside[i - 1] || outside[i + 1]
                            || outside[i - w] || outside[i + w];
                if (!edge) continue;

                int mx = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                int mn = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                if (mx - mn < 40) continue;              // ignore gray/white edges

                sr += p.r; sg += p.g; sb += p.b; n++;
            }

        tex.filterMode = FilterMode.Point;               // keep crisp pixel-art (no blur)
        if (n == 0) { tex.Apply(); return tex; }         // no colored rim found

        byte rr = (byte)(sr / n), rg = (byte)(sg / n), rb = (byte)(sb / n);
        byte nr = (byte)(newC.r * 255f), ng = (byte)(newC.g * 255f), nb = (byte)(newC.b * 255f);
        int tol2 = Tol * Tol;

        // 2) Remap pixels close to the detected rim color → the new rarity color,
        //    but ONLY on the OUTER outline (within RimBand of exterior transparency).
        //    Interior art and interior holes are left untouched.
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                var p = px[i];
                if (p.a < 8) continue;

                int dr = p.r - rr, dg = p.g - rg, db = p.b - rb;
                if (dr * dr + dg * dg + db * db > tol2) continue;

                if (!NearOutside(x, y)) continue;

                px[i] = new Color32(nr, ng, nb, p.a);        // preserve edge alpha for AA
            }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
