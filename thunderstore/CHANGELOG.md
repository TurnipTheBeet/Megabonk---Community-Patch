# Changelog

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
