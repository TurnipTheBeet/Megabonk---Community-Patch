# Changelog

## 1.3.12
- Fix Grandma's Secret Tonic and Spicy Meatball stack caps (now enforced via loot pool, was silently ignored)
- Fix Golden Ring not granting banishes when picked up normally (single-arg AddItem overload was unpatched)
- Fix leaderboard crash when server returns 0 entries
- Fix version check flagging newer local mod as outdated (now only warns when server required > local)

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
