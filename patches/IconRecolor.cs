#nullable disable
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Utility;                       // MyColorUtility
using Assets.Scripts.Inventory__Items__Pickups.Items; // ItemData, EItem, EItemRarity

namespace MegabonkCommunityPatch;

// ─────────────────────────────────────────────────────────────────────────
// ICON BACKGROUND + RIM RECOLOR
//
// Each item icon ships with its rarity-colored background/outline BAKED into
// the texture pixels. When we move an item to a new rarity, the square frame
// recolors itself but the baked background/rim stays the old rarity color.
//
// We swap the RawImage's displayed texture: blit the source into a temp
// RenderTexture, read it back into a readable Texture2D, auto-detect the
// actual baked-in rarity color from the edge pixels, and remap ALL pixels in
// the texture matching that color to the NEW rarity color. This ensures we
// completely recolor the entire background, the outer/inner rims, and any
// enclosed background pockets (like inside the magnet's loop) without needing
// complex or fragile spatial gating.
// Built once per item and cached.
// ─────────────────────────────────────────────────────────────────────────
internal static class IconRecolor
{
    // eItem → original rarity, captured in DataManager.Load BEFORE we mutate.
    internal static readonly Dictionary<EItem, EItemRarity> Original = new();

    // eItem → recolored texture (null = build failed, don't retry).
    static readonly Dictionary<EItem, Texture2D> _cache = new();

    internal static bool GetCached(EItem eItem, out Texture2D tex) => _cache.TryGetValue(eItem, out tex);

    internal static Texture2D GetRecolored(EItem eItem)
    {
        GetCached(eItem, out var tex);
        return tex;
    }

    // Euclidean match tolerance (0-255 space) around the auto-detected rarity color.
    // Set to 120: wide enough to catch anti-aliased gradients and semi-transparent glows,
    // but tight enough to avoid bleeding into distinct art colors.
    const int Tol = 120;

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
            var tex = Build(src, MyColorUtility.GetItemRarityColor(oldR), MyColorUtility.GetItemRarityColor(newR));
            _cache[eItem] = tex;
            if (tex != null) id.icon = tex;
        }
        catch (System.Exception e)
        {
            _cache[eItem] = null;                        // cache failure → no retry storm
            Plugin.Log.LogWarning($"[IconRecolor] {eItem}: {e.Message}");
        }
    }

    static Texture2D Build(Texture src, Color oldC, Color newC)
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

        // 0) Flood-fill the "outside" (exterior transparency) from the texture border inward.
        var outside = new bool[w * h];
        var stack   = new Stack<int>();
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

        // 1) Detect saturated edge colors bordering transparency.
        //    Find the one closest to the expected old rarity color — this is the actual baked-in background color
        //    we need to replace. This correctly targets the OLD color even when the outer rim has already been
        //    recolored to the new rarity color.
        long sr = 0, sg = 0, sb = 0; int n = 0;
        byte er = (byte)(oldC.r * 255f), eg = (byte)(oldC.g * 255f), eb = (byte)(oldC.b * 255f);
        int bestDist = int.MaxValue;
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

                int dr = p.r - er, dg = p.g - eg, db = p.b - eb;
                int dist = dr * dr + dg * dg + db * db;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    sr = p.r; sg = p.g; sb = p.b; n = 1;
                }
                else if (dist == bestDist)
                {
                    sr += p.r; sg += p.g; sb += p.b; n++;
                }
            }

        tex.filterMode = FilterMode.Point;
        if (n == 0) { tex.Apply(); return tex; }

        byte rr = (byte)(sr / n), rg = (byte)(sg / n), rb = (byte)(sb / n);
        byte nr = (byte)(newC.r * 255f), ng = (byte)(newC.g * 255f), nb = (byte)(newC.b * 255f);
        int tol2 = Tol * Tol;

        // 2) Global color replacement: remap any pixel matching the detected old rarity color to the new rarity color.
        //    Covers the entire background, outer/inner rims, and any enclosed pockets (like inside the magnet loop).
        for (int i = 0; i < px.Length; i++)
        {
            var p = px[i];
            if (p.a < 8) continue; // preserve transparency

            int dr = p.r - rr, dg = p.g - rg, db = p.b - rb;
            if (dr * dr + dg * dg + db * db <= tol2)
            {
                px[i] = new Color32(nr, ng, nb, p.a); // preserve original alpha (glow)
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
}
