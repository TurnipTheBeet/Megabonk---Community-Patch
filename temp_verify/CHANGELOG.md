# Changelog

## 1.4.2
- Leaderboard tabs renamed: Global → "Weekly" (resets each week); the repurposed Friends tab is now "Lifetime" — a permanent top-100 that survives the weekly reset. Scrollable.
- Lifetime character filter: icon button cycles All → each character. The board is fetched once and sliced locally, so switching is instant.
- Leaderboard avatars: Lifetime and Personal tabs show your Steam avatar, cached so scrolling doesn't re-fetch.
- Leaderboard fixes: empty-board crash, Personal data sticking on the Weekly tab, the loading spinner lingering after data arrived, and a launch slowdown from repeated server fetches.
- Fixed the Personal leaderboard tab displaying duplicate entries for the same character when the server returns multiple records. It now properly deduplicates the list and only displays the absolute best score per character.
- Bombus is now a pure weapon-damage check: only player weapons hurt him (items, auras, executes don't apply), with HP raised so a crit can't one-shot him. Minimap icon size + a per-frame lag fix.
- Game Speed (T) is no longer treated as a cheat — it won't block leaderboard submission.
- F1 mod menu is OFF by default (enable under Settings ▸ Community Patch). New "Give 1 of Every Item" cheat button.
- Bob's Lantern fire-rate buff trimmed (2x → 1.5x) to reduce explosion stutter.
- Scaling Auto-Upgrade: fixed a level-up that could be eaten with no pick; Mythic rarity now ranks above Legendary in pick priority; maxed gear is never picked.
- SFX volume sliders no longer scale the player-getting-hit or chest-opening sounds.
- Performance: fixed a severe endgame FPS collapse — mod hooks on damage methods fired ~300k times/sec deep into overtime, starving the GC. They now install only while needed (Bombus alive, Instakill on, BT Dagger fired). Plus an optional Profiler toggle and a per-frame budget for Priority Targeting.
- Performance: first F1 menu open no longer hitches, and menu drawing cost is roughly halved while any mod window is open.
- Performance: hot-path mod hooks (Cactus scaling, enemy-speed, Priority Targeting) now install/uninstall on demand instead of running idle.
- The warning banners (update available / other mods detected / mod menu used) merged into one "Notices" panel that can be dragged off the run timer and resized.
- All mod windows now remember position and size across game restarts.
- Typing the F1 menu password no longer moves the character — game keyboard input is muted while the prompt is open.

## 1.4.1
- Added Map Scanner: pick how many map features you want, then auto-reroll the run until a map matches and it stops + pauses. Criteria: Moais, Shady Guys, a combined Moai+Shady count, Boss Curses (exact match), total Microwaves, and Microwaves by tier (Common/Rare/Epic/Legendary). Two rebindable hotkeys in the Community Patch settings tab — F4 opens the window, F5 starts/stops the scan. Runs fully in-process (reads the game's own interactable counts and uses its own restart, no external tool).
- Added a native "Community Patch" tab in the game's Settings menu: rebind every mod hotkey, and adjust the new sliders below — no config-file editing needed.
- Added three SFX volume sliders (Weapon / Hit / Item) in the settings tab, so you can tune or mute combat and item sounds independently of the game's master volume. (Note: some on-hit item effects share the game's generic impact sound and are scaled by the Hit slider.)
- Added a Mod Menu Opacity slider (settings tab) to set the background opacity of the mod's own windows.
- Added Priority Targeting: replaces auto-aim target selection with a scorer that prioritises bosses/elites, then the nearest enemy you can kill soonest (avoids leaving low-HP stragglers alive). Toggle hotkey in the settings tab.
- Added Scaling Auto-Upgrade: auto-picks level-up choices using a bucket system that favours run-defining scaling (new weapons/tomes & legendaries first, then XP/Difficulty, then luck/weapons), weighted by rarity and endgame stat importance. Comes with an Auto-Upgrade Log window (newest-first) showing what it picked and over what. Difficulty (Cursed) stops being prioritised once it hits the 600 effective cap.
- Added Smart Skip Chest Animation: auto-enables chest-open skipping when you run out of banishes, turns it back off when you gain a Golden Ring, and resets after each run.
- Added Fast Fall: hold the bind (default Left Ctrl) while airborne to drop faster.
- Added a Game Speed hotkey (default T) that toggles between 1x and 2x. Counts as a cheat, so it blocks leaderboard submission for that run.
- Added an Effects Opacity hotkey (default F11) that toggles the game's Settings > Effects particle opacity between 0% and 100%.
- Power Gloves: on-hit damage buffed roughly 9x (~22 → ~200 per hit) so it's actually useable.
- Rarity changes: Golden Shield moved Rare → Epic; Slurp Gloves moved Epic → Rare. Their baked rim colors are recolored to match (now correctly limited to the outer outline only).
- Key: chest open chance per stack changed to 15% (from the previous 20%), tooltip updated to match.
- Reorganized default hotkeys to run F1–F9 in the order they appear in the settings tab, skipping F10 (it opens the in-game console). All binds are still fully rebindable.
- Removed the "Restart Run / Reroll Map" button from the mod menu (redundant — the game already has a restart option).
- Leaderboard moved to a dedicated VPS host (more reliable uptime); personal and global submission unchanged.

## 1.4.0
- Fixed fire-rate buffs that weren't applying: Black Hole (3×), Mine, Hero Sword, Poison Flask, Sword, and Corrupt Sword (2× each). The previous tweak only lowered the in-burst projectile spacing, which has no effect on these single-shot weapons; now scales the actual cooldown/burst timing so they really fire faster.
- Reverted the weapon/tome shop slot changes back to vanilla (base 2, buy up to 4). The earlier 1-slot-minimum change wrote an out-of-range shop level into the save, so after switching versions vanilla read it as a 5th tome/weapon slot even with the mod removed. Note: saves already affected need a one-time refund + rebuy of the slots in vanilla to clear it.
- Fixed item rarity rim color not updating for items we moved to a new rarity (e.g. Sucky Magnet Legendary→Epic, Backpack Rare→Common). The baked rim is now recolored at the source, so it shows correctly on every UI surface — unlocks menu, in-run inventory/pause menu, Shady Guy shop, and the world-drop/HUD icon.

## 1.3.19
- Reverted the weapon/tome shop slot changes back to vanilla (base 2, buy up to 4). The earlier 1-slot-minimum change wrote an out-of-range shop level into the save, so after switching versions vanilla read it as a 5th tome/weapon slot even with the mod removed. Note: saves already affected need a one-time refund + rebuy of the slots in vanilla to clear it.
- Fixed item rarity rim color not updating for items we moved to a new rarity (e.g. Sucky Magnet Legendary→Epic, Backpack Rare→Common). The baked rim is now recolored at the source, so it shows correctly on every UI surface — unlocks menu, in-run inventory/pause menu, Shady Guy shop, and the world-drop/HUD icon.

## 1.3.18
- Turbo Juice: now activates while you are moving instead of standing still (movement detected via player velocity, frame-rate independent)
- Idle Juice renamed to Turbo Juice
- Quin's Mask: size cap added (baseRadius 4, radiusPerAmount 2, maxRadius 16 — max 6 stacks; pool slot removed when cap reached)
- BOMBUS overhaul: now killable (invulnerability removed, debuff immunity kept); first spawn at 15 min overtime then every 5 min, stacking (multiple alive at once); boss-tanky HP (20x the bee's natural overtime-scaled HP); gets faster over time and the smaller it shrinks (up to ~2x); drops a Corrupt chest on death
- Corrupt chest fix: the game's evil/corrupt chest was missing its mesh and material (rendered magenta) — visuals now filled from the normal chest so it displays correctly; Corrupted-rarity loot pool is empty in the live game, so corrupt chests now give a guaranteed Golden Ring
- Corrupt chest now spawns on the ground (downward raycast) instead of floating at the bee's center; bee corpse colliders disabled so the ground is detected correctly
- Fix: winning the run by entering the final-stage portal now records your score to the leaderboard (the game's win-path upload fired too late to reach the server; we now upload the moment you commit to the portal)
- Chaos/Gamble stat toggles (F3): Duration and Movement Speed are now toggleable in the pool (both on by default — toggle off to remove)
- Mod menus (F1 cheat menu, F3 Chaos stats): open at double size by default and are now resizeable — drag the title bar to move, drag the ↘ grip in the bottom-right corner to scale
- Damage chart (F2): now scrollable via mouse wheel; renders above the pause menu so it's readable while paused; fixed it showing only one weapon and blocking the death-screen Continue button when opened mid-run- BOMBUS: minimap icon size fixed (no longer oversized); first arrival now delayed by +5 minutes per stage on later stages
- Bow: fire rate buffed (cooldown 0.92s → 0.45s, roughly double the shots per second)
- Sniper: post-burst cooldown halved (~2.0s → ~1.5s per shot; burst/audio unchanged); minBurstInterval lowered to 0.1 for tighter bursts when stacking projectiles; damage unchanged
- Spiky Shield: moved to the Legendary pool (was Epic) and armor per stack buffed 10 → 50 (fed through the game's hyperbolic armor curve, so damage reduction still caps below 100%)
- Energy Core: moved to the Rare pool (was Legendary) — weak item, better fit
- Burst fire rate buffed (lower minBurstInterval = tighter bursts): Black Hole 3×, and 2× for Mine, Hero Sword, Poison Flask, Sword, and Corrupt Sword
- Scythe: attack rate roughly doubled (endCooldown 0.85s → 0.425s and burstTime 1.5s → 0.75s; burstTime is the dominant term in the cooldown formula)
- Roberto (Hoarder passive): now also gains +0.5% elite damage per level
- Item icons: when an item is moved to a new rarity, its baked rarity-colored outline is now recolored to match the new rarity

## 1.3.17
- Add BOMBUS: giant invulnerable Bee boss (15× scale) that spawns at 28 minutes into overtime with a boss HP bar and one-shot contact damage
- Za Warudo: track cumulative pickups per run; remove from loot pool after 25 total received
- Leaderboard: scores now only submitted when a new personal best is set — personal bests cached on main menu load
- Leaderboard: unauthorized mods detected at startup via BepInEx chainloader reflection; score upload blocked and warning banner shown if any non-patch mods are loaded
- F1 menu: show warning banner when any cheat feature is used, informing player that leaderboard is disabled for the run; banner hidden on main menu

## 1.3.16
- Cactus: thorn range now scales with player size (SphereCast maxDistance × SizeMultiplier)
- Cactus: 4 projectiles per stack (up from default)

## 1.3.15
- Fix Key and Echo Shard missing from loot pool (save deactivation state was not overridden)
- Fix Backpack locale bug: set projPerAmount directly (was doubling amount — broke non-English locales)
- Fix Brass Knuckles size cap: move write to postfix so method can't overwrite it
- Golden Ring: restore 1/400 → 1/128 drop chance via safe Harmony patch (no VirtualProtect)
- DurationMultiplier removed from stat blacklists (Chaos, Gamble, Shrines)
- Leaderboard: Personal tab added — shows your best run per character, scrollable
- Leaderboard: mouse wheel scrolling on Global, Friends, and Personal tabs
- Leaderboard: rank numbers scroll correctly with entries
- Leaderboard: your entry no longer pins/locks when scrolling past it in Global
- Leaderboard: personal data pre-fetched on main menu load so it's ready instantly

## 1.3.14
- Remove VirtualProtect usage (Thunderstore TOS compliance)
- Golden Ring chest drop chance reverts to 1/400 (will be restored via Harmony patch in a future update)
- Add AI disclosure to README

## 1.3.13
- Golden Ring: chest drop chance increased from 1/400 to 1/128
- Remove MoveSpeedMultiplier from stat blacklists (Chaos, Gamble Tome, Dicehead, Charge Shrines)

## 1.3.12
- Fix Grandma's Secret Tonic and Spicy Meatball stack caps (now enforced via loot pool, was silently ignored)
- Fix Golden Ring not granting banishes when picked up normally (single-arg AddItem overload was unpatched)
- Fix leaderboard crash when server returns 0 entries
- Fix version check flagging newer local mod as outdated (now only warns when server required > local)
- Fix stale server URL in config not updating after mod update

## 1.3.11
- Key: chest open chance increased from 10% to 20% per stack
- Cursed Doll: fix maxNumCursesPerCheck (was 5, now matches enemiesCursedPerDoll = 7)
- Remove MaxHealth and PickupRange from stat blacklist

## 1.3.10
- Fix knockback resistance scaling removal — replace crashing method patch with direct memory write
- Grandma's Secret Tonic: force baseRadius=4, radiusPerAmount=2, maxRadius=16 (max 6 stacks)
- Spicy Meatball: baseRadius=8, radiusPerAmount=4, maxRadius=32 (2× Grandma's values)
- All weapons: +10% crit chance and +20% crit damage added to upgrade pool
- Add MoveSpeedMultiplier to stat blacklist
- Greed Shrine: fix XP bonus to use Addition modifier (matches Exp Tome bucket, no double-dip)
- Remove Clock Powerup time-freeze patch (too strong)

## 1.3.9
- Fix crash spam in GetKnockbackResistanceMultiplierAddition causing severe FPS drops during combat

## 1.3.8
- Fix combat scaling (2× HP/damage ramp, knockback resistance removal) — was silently broken due to IL2CPP static field access bug
- Remove leftover debug logging patches (Lightning Orb, Bluetooth Dagger, Backpack) that were causing significant per-frame log spam and GC pressure
- Fix Tony McZooms passive stacking duplicate handlers across runs

## 1.3.7
- Remove Spicy Meatball patch (was doubling max size from 8m to 16m, causing severe lag)

## 1.3.6
- Fix forced pool items (Sucky Magnet etc.) not appearing in runs due to save deactivation state

## 1.3.5
- Force Battery, Skuleg, OldMask, BrassKnuckles, DemonicBlood, IdleJuice, SuckyMagnet into loot pool; toggle hidden (always active)
- Show in-game warning banner if mod is outdated (server-driven version check on main menu load)
- ServerUrl added to config (hosts only — do not change)
- Leaderboard fixes and stability improvements

## 1.3.4
- Leaderboard now requires current mod version — outdated clients rejected

## 1.3.3
- Password protect F1 debug menu
- Mark mod as WIP in README

## 1.3.2
- Hardcode stat blacklists and leaderboard server — no longer user-configurable

## 1.3.1
- Fix in-game mod version display (was showing 1.1.0)

## 1.3.0
- Fix clock timer console spam
- Show mod version in main menu next to game version
- Tony McZooms (Zap passive): +0.25 projectiles per level
- Golden Shield: remove reduced gold penalty on Kevin self-damage
- Spicy Meatball: pool slot removed dynamically when size cap (16m) is reached, accounts for size stat
- Grandma's Secret Tonic: pool slot removed dynamically when size cap is reached
- Hardcode item caps and size cap removal as always-on
- Hardcode stat blacklists, remove dropdowns from F1 menu
- Remove item cap and size cap dropdowns from F1 menu

## 1.2.0
- Space Noodle free-aim rework

## 1.1.0
- Space Noodle rework (StartLaser timing + StopLaser AoE finisher)
