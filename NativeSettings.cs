using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using BepInEx.Configuration;
using System.Text;
using System.Collections.Generic;

namespace MegaBonkMod;

// ─────────────────────────────────────────────────────────────────────────
// NATIVE SETTINGS INJECTION  (isolated on purpose — game UI is obfuscated and
// reflection-driven, so this is the piece most likely to break on a game
// update. Keep it self-contained for easy repair.)
//
// Adds a "Community Patch" tab to the game's own Options screen containing our
// rebindable hotkeys. Built by cloning the game's existing tab button + tab
// content + a button row, then wiring tab-switching and key-capture ourselves
// (so we don't depend on the obfuscated row-builder).
//
// Structure (from a live hierarchy dump):
//   W_Settings (Settings component)
//     WindowLayers/Content/Settings/SettingsMask/{Game,Video,Controls,Audio,Visuals,Other}
//       each = ScrollRect with <Tab>_Content (VerticalLayoutGroup) + Scrollbar
//       a row = Button+MyButtonNormal, label at Content/Left/Text (TMP)
//     WindowLayers/TabButtons/ButtonGroups/{B_Game..B_Other}  (MyButtonTabs)
// ─────────────────────────────────────────────────────────────────────────
internal static class NativeSettings
{
    static readonly BepInEx.Logging.ManualLogSource Log =
        BepInEx.Logging.Logger.CreateLogSource("MegaBonkMod.NativeSettings");

    const string TabButtonName = "B_CommunityPatch";

    // refs kept so we can reset our tab when the Settings window closes
    static GameObject _ourTab;
    static MyButtonTabs _ourMbt;
    static Settings _sm;          // settings instance, for lazy keycap-template capture

    // rows (keycap + bound entry), so keycaps can be painted when the tab is shown
    static List<(KeyDisplay kd, ConfigEntry<KeyCode> entry)> _rows;

    // keycap visual template, copied from a live native control row. SetNonGlyph
    // only enables KeyDisplay.background + sets text; the dark keycap *texture* is
    // applied at runtime by the game (InputSettingNew, which we strip), so we must
    // copy it ourselves or our keycaps render as blank white RawImages.
    static bool    _kcHas;
    static Texture _kcKdTex;        // KeyDisplay.background texture
    static Color   _kcKdColor   = Color.white;
    static Texture _kcBtnTex;       // InputBtn0 RawImage texture (outer keycap)
    static Color   _kcBtnColor  = Color.white;
    static Color   _kcTextColor = Color.white;

    // ── key capture state (polled by ModGui.Update via TickCapture) ──
    static bool _capturing;
    static ConfigEntry<KeyCode> _capEntry;
    static KeyDisplay _capKd;
    static string _capName;
    static int _capStartFrame;

    // ───────────────────────────── injection ─────────────────────────────
    internal static void TryInject(Settings sm)
    {
        try
        {
            if (sm == null) return;
            _sm = sm;
            var root = sm.gameObject;

            var nav = root.GetComponentInChildren<ButtonNavigationSelectionOnly>(true);
            if (nav == null) { Log.LogWarning("[CP] no ButtonNavigationSelectionOnly found"); return; }
            var buttonsParent = nav.transform;

            // already injected?
            for (int i = 0; i < buttonsParent.childCount; i++)
                if (buttonsParent.GetChild(i).name == TabButtonName) return;

            if (sm.otherContent == null) { Log.LogWarning("[CP] otherContent null"); return; }
            if (sm.controlPrefabNew == null) { Log.LogWarning("[CP] controlPrefabNew null"); return; }

            // capture keycap visuals straight from the prefab asset (its serialized
            // RawImage textures survive regardless of active state — unlike the live
            // controlContent rows which are blank until the Controls tab is shown)
            CaptureKeycapFromPrefab(sm.controlPrefabNew);
            var otherNode    = sm.otherContent.parent;       // the "Other" ScrollRect node
            var settingsMask = otherNode.parent;             // SettingsMask

            // Build everything under an INACTIVE, DETACHED holder (no parent under
            // the settings window). Two reasons:
            //  1. Object.Instantiate of an active object runs the clones'
            //     Awake/OnEnable immediately; building inactive avoids that.
            //  2. Window caches its button list via GetComponentsInChildren<MyButton>
            //     (includeInactive = true) over the whole window subtree. If our
            //     transient clones (esp. the cloned "Other" rows we delete) lived
            //     under the window when that list was built, destroying them would
            //     leave dangling MyButton refs -> NRE in Window.UnfocusWindow on
            //     close (cursor freeze). Keeping the holder detached means nothing
            //     we destroy is ever a child of the window.
            var holder = new GameObject("CP_Holder");
            holder.SetActive(false);
            var holderT = holder.transform;

            // ── clone the tab content (inactive) ──
            var ourTab = Object.Instantiate(otherNode.gameObject, holderT);
            ourTab.name = "CP_Tab";

            var sr = ourTab.GetComponent<ScrollRect>();
            var content = sr != null && sr.content != null ? sr.content : ourTab.transform;

            for (int i = content.childCount - 1; i >= 0; i--)   // drop the cloned "Other" rows
                Object.DestroyImmediate(content.GetChild(i).gameObject);  // immediate: must be gone before reparent

            // ── build a row per hotkey from the native control-row prefab ──
            _rows = new List<(KeyDisplay, ConfigEntry<KeyCode>)>();
            foreach (var (label, entry) in Hotkeys.All())
                BuildRow(sm.controlPrefabNew, content, label, entry);

            // ── slider rows (cloned from the native audio slider prefab) ──
            BuildSliderRow(sm.sliderPrefab, content, "Weapon SFX Volume",
                () => WeaponSfxVolume.Weapon, v => WeaponSfxVolume.Weapon = v);
            BuildSliderRow(sm.sliderPrefab, content, "Hit SFX Volume",
                () => WeaponSfxVolume.Hit, v => WeaponSfxVolume.Hit = v);
            BuildSliderRow(sm.sliderPrefab, content, "Item SFX Volume",
                () => WeaponSfxVolume.Item, v => WeaponSfxVolume.Item = v);
            BuildSliderRow(sm.sliderPrefab, content, "Mod Menu Opacity",
                () => UiTheme.Opacity, v => UiTheme.Opacity = v);

            // ── clone the tab button (inactive) ──
            var btnTemplate = buttonsParent.childCount > 0 ? buttonsParent.GetChild(0).gameObject : null;
            if (btnTemplate == null) { Log.LogWarning("[CP] no tab-button template"); Object.Destroy(holder); return; }

            var ourBtnGo = Object.Instantiate(btnTemplate, holderT);
            ourBtnGo.name = TabButtonName;

            var mbt = ourBtnGo.GetComponent<MyButtonTabs>();

            var btnTmp = ourBtnGo.GetComponentInChildren<TMP_Text>(true);
            if (btnTmp != null) { StripLocalizer(btnTmp.gameObject); btnTmp.text = "Mod"; }

            // ── collect the game's tabs (still the only children of buttonsParent) ──
            var sixTabs = new List<MyButtonTabs>();
            for (int i = 0; i < buttonsParent.childCount; i++)
            {
                var m = buttonsParent.GetChild(i).GetComponent<MyButtonTabs>();
                if (m != null) sixTabs.Add(m);
            }

            // ── wire tab switching ourselves ──
            var ourBtn = ourBtnGo.GetComponent<Button>();
            if (ourBtn != null)
            {
                ourBtn.onClick.RemoveAllListeners();
                ourBtn.onClick.AddListener((UnityAction)(() => ShowTab(ourTab, mbt, sixTabs)));
            }
            foreach (var g in sixTabs)
            {
                var b = g.GetComponent<Button>();
                if (b != null) b.onClick.AddListener((UnityAction)(() => HideTab(ourTab, mbt)));
            }

            // ── move the finished objects into the live hierarchy ──
            ourTab.transform.SetParent(settingsMask, false);
            ourTab.SetActive(false);                          // hidden until our tab is selected
            if (mbt != null) mbt.associatedContent = ourTab;
            ourBtnGo.transform.SetParent(buttonsParent, false);
            ourBtnGo.SetActive(true);                         // MyButtonTabs Awakes cleanly now
            Object.Destroy(holder);

            _ourTab = ourTab;
            _ourMbt = mbt;

            Log.LogInfo("[CP] Community Patch tab injected.");
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] inject failed: {e}"); }
    }

    // When the Settings window closes, make sure our tab isn't the one left active
    // (the game's close/restore logic shouldn't have to deal with our injected tab).
    internal static void OnSettingsClosed()
    {
        try
        {
            if (_ourTab != null) _ourTab.SetActive(false);
            if (_ourMbt != null && _ourMbt.background != null) _ourMbt.background.color = _ourMbt.defaultColor;
            if (_capturing) { _capturing = false; Hotkeys.Capturing = false; _capEntry = null; _capKd = null; _capName = null; }
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] OnSettingsClosed: {e.Message}"); }
    }

    // Build one hotkey row by cloning the game's native control-row prefab, so it
    // matches the Controls tab (flat row, keycap box on the right).
    static void BuildRow(GameObject prefab, Transform parent, string label, ConfigEntry<KeyCode> entry)
    {
        try
        {
            var rowGo = Object.Instantiate(prefab, parent);
            rowGo.name = "CP_Row_" + label.Replace(" ", "");
            rowGo.SetActive(true);

            // kill the Rewired-driven script so it can't clobber our text on enable
            StripComp(rowGo, "InputSettingNew");

            // Strip ALL MyButton-derived components (MyButtonSetting / MyButtonNormal
            // / base MyButton). Critical: Window builds its focus list from
            // GetComponentsInChildren<MyButton>(includeInactive:true) and calls
            // SetFocus()->GetButton()->GetComponent on each. Our rows don't need a
            // MyButton (we wire our own Button.onClick), and leaving one in makes the
            // window try to focus a row whose state we've torn up -> NRE on close.
            StripMyButtons(rowGo);

            var t = rowGo.transform;

            // label (left)
            var leftT = t.Find("Content/Left/Text (TMP)");
            var leftText = leftT != null ? leftT.GetComponent<TMP_Text>() : null;
            if (leftText != null) { StripLocalizer(leftText.gameObject); leftText.text = label; }

            // keycap (right): show InputBtn0, hide the extra binds + mouse glyphs.
            // Keep the KeyDisplay component and drive it with SetKey so we get the
            // real keycap glyph/sprite (not a blank white box).
            var interact = t.Find("Content/Right/InteractSetting");
            KeyDisplay keyDisp = null;
            if (interact != null)
            {
                var btn0 = interact.Find("InputBtn0");
                if (btn0 != null)
                {
                    btn0.gameObject.SetActive(true);
                    StripComp(btn0.gameObject, "Button");   // let clicks fall through to the row

                    // InputBtn0 has THREE stacked layers in the keycap area:
                    //   - InputBtn0 RawImage  (245x60, white)  ← big white block
                    //   - OutlineMask  Image  (241x56, white)  ← big white block
                    //   - KeyDisplay   RawImage(47x38)         ← the real small keycap
                    // The game hides the two big ones at runtime (InputSettingNew, which
                    // we strip) and shows only the small KeyDisplay. We must do the same
                    // or the whole right column renders as one solid white bar.
                    // Native keeps InputBtn0 as a subtle dark *cell* behind the keycap
                    // (the inner rectangle separating the keycap from the wide row).
                    // Re-tint it dark/translucent instead of leaving it a white block.
                    var btnRaw = btn0.GetComponent<RawImage>();
                    if (btnRaw != null) { btnRaw.color = new Color(0f, 0f, 0f, 0.25f); btnRaw.enabled = true; }
                    HideChild(btn0, "OutlineMask");

                    var kd = btn0.Find("KeyDisplay");
                    if (kd != null) keyDisp = kd.GetComponent<KeyDisplay>();
                }
                HideChild(interact, "InputBtn1");
                HideChild(interact, "GlyphContainer1");
                HideChild(interact, "GlyphContainer1 (1)");
            }
            // keycap is painted later (in ShowTab), once the row is active
            if (keyDisp != null)
            {
                BuildKeycapVisual(keyDisp);   // build the 6-layer rounded keycap now
                _rows?.Add((keyDisp, entry));
            }

            // whole-row click → rebind
            var btn = rowGo.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                var capName = label; var capEntry = entry; var capKd = keyDisp;
                btn.onClick.AddListener((UnityAction)(() => BeginCapture(capName, capEntry, capKd)));
            }
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] BuildRow '{label}': {e.Message}"); }
    }

    // Build a 0..100% slider row by cloning the game's native audio slider prefab,
    // stripping its CFSettings-coupled SliderSetting driver, and wiring the raw
    // UnityEngine.UI.Slider straight to our config getter/setter.
    static void BuildSliderRow(GameObject prefab, Transform parent, string label,
                               System.Func<float> get, System.Action<float> set)
    {
        try
        {
            if (prefab == null) { Log.LogWarning("[CP] sliderPrefab null"); return; }

            var rowGo = Object.Instantiate(prefab, parent);
            rowGo.name = "CP_Slider_" + label.Replace(" ", "").Replace("/", "");
            rowGo.SetActive(true);

            // grab serialized refs from the native driver BEFORE stripping it
            var ss = rowGo.GetComponentInChildren<SliderSetting>(true);
            Slider slider = ss != null && ss.slider != null
                ? ss.slider : rowGo.GetComponentInChildren<Slider>(true);
            TMP_InputField valueText = ss != null ? ss.valueText : null;

            // detach native drivers so nothing writes the result back into CFSettings
            StripComp(rowGo, "SliderSetting");
            StripMyButtons(rowGo);

            // label (left) — same path as the control rows, fallback to first TMP
            var leftT = rowGo.transform.Find("Content/Left/Text (TMP)");
            var leftText = leftT != null ? leftT.GetComponent<TMP_Text>() : null;
            if (leftText == null)
            {
                foreach (var tmp in rowGo.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (valueText != null && tmp.gameObject == valueText.gameObject) continue;
                    leftText = tmp; break;
                }
            }
            if (leftText != null) { StripLocalizer(leftText.gameObject); leftText.text = label; }

            // the value field is display-only now (no native driver to parse typing)
            if (valueText != null) valueText.interactable = false;

            if (slider == null) { Log.LogWarning("[CP] slider row has no Slider"); return; }

            slider.wholeNumbers = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(get());
            if (valueText != null) valueText.text = Mathf.RoundToInt(get() * 100f) + "%";

            slider.onValueChanged.RemoveAllListeners();
            var setLocal = set; var vtLocal = valueText;
            slider.onValueChanged.AddListener((UnityAction<float>)(v =>
            {
                setLocal(v);
                if (vtLocal != null) vtLocal.text = Mathf.RoundToInt(v * 100f) + "%";
            }));
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] BuildSliderRow '{label}': {e.Message}"); }
    }

    static void HideChild(Transform parent, string name)
    {
        var c = parent.Find(name);
        if (c != null) c.gameObject.SetActive(false);
    }

    static void StripComp(GameObject go, string typeName)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            string n; try { n = c.GetIl2CppType().Name; } catch { continue; }
            if (n == typeName) Object.DestroyImmediate(c);   // immediate: deferred destroy leaves the comp live for this frame's window scan
        }
    }

    // Destroy any component that is (or derives from) MyButton, so the row never
    // appears in a Window's GetComponentsInChildren<MyButton> focus list.
    // DestroyImmediate is essential: a deferred Object.Destroy leaves the MyButton
    // alive until end-of-frame, so the window's focus list (built this same frame)
    // captures it, then it dies -> dangling component -> NRE in UnfocusWindow.
    static void StripMyButtons(GameObject go)
    {
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            try { if (c.TryCast<MyButton>() != null) Object.DestroyImmediate(c); } catch { }
        }
    }

    // Find a live, fully-rendered native control row and copy its keycap visuals
    // (the dark rounded RawImage textures + text color). controlContent is already
    // populated at Settings.OnEnable, so this normally succeeds the first time the
    // tab is shown. Captured once, reused for every row.
    // Capture keycap textures/colors from the prefab asset itself. Walks the same
    // path BuildRow uses (Content/Right/InteractSetting/InputBtn0) and reads the
    // serialized RawImage textures. This is the reliable source: the prefab keeps
    // its serialized state whether or not any live row has been shown.
    static void CaptureKeycapFromPrefab(GameObject prefab)
    {
        if (_kcHas || prefab == null) return;
        try
        {
            var btn0 = prefab.transform.Find("Content/Right/InteractSetting/InputBtn0");
            if (btn0 == null) { Log.LogWarning("[CP] prefab InputBtn0 not found"); return; }

            var btnImg = btn0.GetComponent<RawImage>();
            var kdT    = btn0.Find("KeyDisplay");
            var kd     = kdT != null ? kdT.GetComponent<KeyDisplay>() : null;
            var bg     = kd != null ? kd.background : null;

            if (bg != null)     { _kcKdTex = bg.texture;  _kcKdColor  = bg.color; }
            if (btnImg != null) { _kcBtnTex = btnImg.texture; _kcBtnColor = btnImg.color; }
            if (kd != null && kd.text != null) _kcTextColor = kd.text.color;

            // mark captured if we got at least one texture OR a non-white btn color
            if ((bg != null && bg.texture != null) || (btnImg != null && btnImg.texture != null))
                _kcHas = true;
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] CaptureKeycapFromPrefab: {e.Message}"); }
    }

    // Polled every frame from ModGui.Update. The dark/rounded keycap texture is
    // only present on a control row while the Controls tab is actually rendering
    // (it's runtime-assigned, never serialized). So we watch for the Controls tab
    // being active and snapshot a live, painted keycap then.
    internal static void PollKeycapCapture()
    {
        if (_kcHas || _sm == null) return;
        try
        {
            var cc = _sm.controlContent;
            if (cc == null || !cc.gameObject.activeInHierarchy) return;   // only while Controls shown

            var kds = cc.GetComponentsInChildren<KeyDisplay>(true);
            if (kds == null) return;

            foreach (var kd in kds)
            {
                if (kd == null || !kd.gameObject.activeInHierarchy) continue;
                var bg = kd.background;

                // capture the first keycap that actually has a background texture
                if (bg != null && bg.texture != null)
                {
                    _kcKdTex = bg.texture; _kcKdColor = bg.color;
                    if (kd.text != null) _kcTextColor = kd.text.color;
                    _kcHas = true;
                    RepaintOurRows();
                    break;
                }
            }
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] PollKeycapCapture: {e.Message}"); }
    }

    // Re-run SetKey + paint on our rows (used after a late capture while our tab may
    // already be open).
    static void RepaintOurRows()
    {
        if (_rows == null) return;
        foreach (var r in _rows)
            try { if (r.kd != null) { r.kd.SetKey(r.entry.Value); ApplyKeycapVisual(r.kd); } }
            catch { }
    }

    static void CaptureKeycapTemplate()
    {
        if (_kcHas || _sm == null) return;
        try
        {
            Transform cc = _sm.controlContent;
            if (cc == null) return;

            var kds = cc.GetComponentsInChildren<KeyDisplay>(true);
            if (kds == null) return;

            foreach (var kd in kds)
            {
                if (kd == null) continue;
                var parent = kd.transform.parent;                         // InputBtn0
                var btnImg = parent != null ? parent.GetComponent<RawImage>() : null;
                var bg = kd.background;

                if (!_kcHas)
                {
                    bool good = (bg != null && bg.texture != null) || (btnImg != null && btnImg.texture != null);
                    if (good)
                    {
                        if (bg != null)     { _kcKdTex = bg.texture;  _kcKdColor = bg.color; }
                        if (btnImg != null) { _kcBtnTex = btnImg.texture; _kcBtnColor = btnImg.color; }
                        if (kd.text != null) _kcTextColor = kd.text.color;
                        _kcHas = true;
                    }
                }
            }
            if (!_kcHas) Log.LogWarning($"[CP] no textured keycap among {kds.Length} KeyDisplays.");
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] CaptureKeycapTemplate: {e.Message}"); }
    }

    // Keyboard keycaps (F1/F2/LeftControl…) have NO runtime texture — SetNonGlyph
    // only enables a blank white RawImage + sets text. So we BUILD the keycap from
    // scratch as stacked layers, matching the native rounded look:
    //   (1) background (the row itself)
    //   (2)+(3) two horizontal transparent dark "bars" that darken the area
    //   (4) white rounded square  ← the keycap body / border
    //   (5) dark rounded square inset inside (4) → leaves a white border ring
    //   (6) white text on top
    static Sprite _capSprite;
    static bool   _capTried;

    // Runtime texture generation is a no-go on this IL2CPP build:
    //   - Texture2D.SetPixels32 → "Method unstripping failed" (null sprite, square)
    //   - ImageConversion.LoadImage → hard native crash on Settings open
    // So instead we REUSE a rounded 9-sliced sprite the game already has loaded.
    // Any UI sprite with a non-zero border (i.e. 9-sliceable) gives rounded corners.
    static Sprite CapSprite()
    {
        if (_capTried) return _capSprite;
        _capTried = true;
        try
        {
            var all = Resources.FindObjectsOfTypeAll<Sprite>();
            if (all == null) { Log.LogWarning("[CP] no sprites loaded"); return null; }

            Sprite best = null;
            foreach (var s in all)
            {
                if (s == null) continue;
                Vector4 b; try { b = s.border; } catch { continue; }
                if (b == Vector4.zero) continue;            // need 9-slice border for clean rounding
                string n = s.name ?? "";
                string nl = n.ToLowerInvariant();

                bool keyish = nl.Contains("key") || nl.Contains("input") || nl.Contains("bind");
                bool boxish = nl.Contains("round") || nl.Contains("button") || nl.Contains("btn")
                              || nl.Contains("cell") || nl.Contains("box") || nl.Contains("frame")
                              || nl.Contains("panel") || nl.Contains("bg") || nl.Contains("back");

                if (keyish) { best = s; break; }            // best match — stop
                if (best == null && boxish) best = s;       // fallback candidate
            }

            _capSprite = best;
            if (best == null) Log.LogWarning("[CP] no rounded 9-slice sprite found — keycaps stay square");
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] CapSprite: {e.Message}"); }
        return _capSprite;
    }

    // Add a stretch-fill child Image under parent. inset>0 shrinks it (border ring);
    // inset<0 grows it past the parent (soft outer darkening).
    static Image AddImg(Transform parent, string name, Sprite sprite, Color col, float inset)
    {
        var go = new GameObject(name);
        var rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = col;
        if (sprite != null) img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        return img;
    }

    // Build the stacked keycap layers once, under the KeyDisplay transform.
    static void BuildKeycapVisual(KeyDisplay kd)
    {
        if (kd == null) return;
        try
        {
            var p = kd.transform;
            if (p.Find("CP_CapDark") != null) return;   // already built

            var sp = CapSprite();

            // hide the blank white KeyDisplay background (SetNonGlyph keeps enabling it)
            if (kd.background != null) kd.background.enabled = false;

            // (2)+(3) two transparent dark rounded halos extending past the cap → "darken"
            AddImg(p, "CP_BarA", sp, new Color(0f, 0f, 0f, 0.15f), -4f);
            AddImg(p, "CP_BarB", sp, new Color(0f, 0f, 0f, 0.15f), -2f);
            // (4) white rounded square = keycap body / border
            AddImg(p, "CP_CapWhite", sp, Color.white, 0f);
            // (5) dark rounded square inset → leaves a white ring as the border
            AddImg(p, "CP_CapDark", sp, new Color(0.12f, 0.13f, 0.15f, 1f), 2.5f);

            // (6) text stays on top
            if (kd.text != null)
            {
                kd.text.color = Color.white;
                kd.text.transform.SetAsLastSibling();
            }
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] BuildKeycapVisual: {e.Message}"); }
    }

    // Re-assert keycap state after SetKey (SetNonGlyph re-enables the blank bg and
    // may reset text color, so we override every time the key is (re)painted).
    static void ApplyKeycapVisual(KeyDisplay kd)
    {
        if (kd == null) return;
        try
        {
            if (kd.background != null) kd.background.enabled = false;
            if (kd.text != null)
            {
                kd.text.color = Color.white;
                kd.text.transform.SetAsLastSibling();   // keep text above cap layers
            }
        }
        catch { }
    }

    static void ShowTab(GameObject ourTab, MyButtonTabs ourMbt, List<MyButtonTabs> others)
    {
        try
        {
            foreach (var g in others)
            {
                if (g.associatedContent != null) g.associatedContent.SetActive(false);
                if (g.background != null) g.background.color = g.defaultColor;
            }
            ourTab.SetActive(true);
            if (ourMbt != null && ourMbt.background != null) ourMbt.background.color = ourMbt.selectedColor;

            CaptureKeycapTemplate();   // grab keycap texture/colors from a live native row

            // paint keycaps now the rows are active (SetKey is unreliable while inactive)
            if (_rows != null)
                foreach (var r in _rows)
                    try
                    {
                        if (r.kd == null) continue;
                        r.kd.SetKey(r.entry.Value);       // sets text + enables background/glyph
                        ApplyKeycapVisual(r.kd);          // paint the keycap texture the game would have
                    }
                    catch { }
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] ShowTab: {e.Message}"); }
    }

    static void HideTab(GameObject ourTab, MyButtonTabs ourMbt)
    {
        try
        {
            ourTab.SetActive(false);
            if (ourMbt != null && ourMbt.background != null) ourMbt.background.color = ourMbt.defaultColor;
        }
        catch (System.Exception e) { Log.LogWarning($"[CP] HideTab: {e.Message}"); }
    }

    // LocalizeStringEvent re-applies a localized string on enable, clobbering our
    // text. Remove it so our label sticks.
    static void StripLocalizer(GameObject go)
    {
        try
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                string n; try { n = c.GetIl2CppType().Name; } catch { continue; }
                if (n == "LocalizeStringEvent") Object.Destroy(c);
            }
        }
        catch { }
    }

    // ───────────────────────────── key capture ─────────────────────────────
    static void BeginCapture(string name, ConfigEntry<KeyCode> entry, KeyDisplay kd)
    {
        _capturing = true;
        Hotkeys.Capturing = true;
        _capName = name; _capEntry = entry; _capKd = kd;
        _capStartFrame = Time.frameCount;
        if (kd != null && kd.text != null) kd.text.text = "?";   // prompt while waiting
    }

    // Called every frame from ModGui.Update.
    internal static void TickCapture()
    {
        if (!_capturing) return;
        if (Time.frameCount == _capStartFrame) return;       // skip the click frame

        if (Input.GetKeyDown(KeyCode.Escape)) { EndCapture(_capEntry.Value); return; }

        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (kc == KeyCode.None) continue;
            if ((int)kc >= (int)KeyCode.Mouse0 && (int)kc <= (int)KeyCode.Mouse6) continue; // skip mouse
            if (Input.GetKeyDown(kc)) { _capEntry.Value = kc; EndCapture(kc); return; }
        }
    }

    static void EndCapture(KeyCode k)
    {
        if (_capKd != null) { _capKd.SetKey(k); ApplyKeycapVisual(_capKd); }
        _capturing = false;
        Hotkeys.Capturing = false;
        Hotkeys.LastCaptureFrame = Time.frameCount;   // suppress hotkey actions for the rest of this frame
        _capEntry = null; _capKd = null; _capName = null;
    }
}

// Inject when the Options screen is enabled.
[HarmonyPatch(typeof(Settings), "OnEnable")]
static class Patch_CPTab_Inject
{
    [HarmonyPostfix]
    static void Postfix(Settings __instance) => NativeSettings.TryInject(__instance);
}

// Reset our tab state when the Settings window closes.
[HarmonyPatch(typeof(Settings), "OnDisable")]
static class Patch_CPTab_Close
{
    [HarmonyPostfix]
    static void Postfix() => NativeSettings.OnSettingsClosed();
}
