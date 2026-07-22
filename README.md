# Megabonk Community Patch

> **⚠ Work in Progress** — Actively developed. Expect bugs and frequent updates.

A community-driven balance and quality-of-life mod for Megabonk.

---

## Support

If you enjoy the mod and want to support its development, consider buying me a coffee: https://ko-fi.com/pheeesh

---

## Community Leaderboard

- Replaces the Steam leaderboard with a shared community leaderboard
- Scores submitted automatically at the end of each run
- Friends tab shows server data filtered by your Steam friends
- **Personal tab** — shows your best run per character (scroll with mouse wheel)
- Global/Friends tabs support mouse wheel scrolling with correct rank numbers
- Server rejects submissions from outdated mod versions
- Shows an in-game warning banner if your mod is out of date

---

## Gameplay

- **Fast Fall** — hold the bind (default Left Ctrl, rebindable) while airborne to fall faster
- **Full Heal** — heart powerup fully heals instead of partial
- **Pots** — drop a random powerup instead of always health
- **Golden Ring** — grants +1 banish per ring held
- **Greed Shrine** — also grants +5% XP and +5% luck
---

## Item Tweaks

- **Item Caps** — Anvil (max 2), Overpowered Lamp (max 3), Za Warudo (max 10)
- **Grandma's Secret Tonic** — baseRadius=4, radiusPerAmount=2, maxRadius=16m (max 6 stacks); pool slot removed once cap is reached
- **Spicy Meatball** — baseRadius=8, radiusPerAmount=4, maxRadius=32m (2× Grandma's values)
- **Brass Knuckles** — size cap removed
- **Bob's Lantern** — fire rate doubled; rarity changed to Legendary
- **Backpack** — grants +2 projectiles per stack instead of +1
- **Cursed Doll** — 50% max HP damage (was 30%), curses 7 enemies per doll (was 2); rarity changed to Legendary
- **Green Credit Card** — chest price increase reduced from 10% to 2% per card
- **Echo Shard** — overflow chance rolls extra shards
- **Tony McZooms** — Zap passive grants +0.25 projectiles per level
- **Roberto** — Hoarder passive also grants +0.5% elite damage per level
- **Power Gloves** — on-hit damage buffed ~9× (~22 → ~200 per hit)
- **Key** — chest open chance 15% per stack (tooltip updated)
- **Golden Shield** — removes reduced gold penalty on Kevin self-damage
- **Bluetooth Dagger** — correctly triggers Lightning Orb stun (element fix)
- **All Weapons** — +10% crit chance and +20% crit damage added to upgrade pool
- **Bow / Revolver** — +2 projectiles per level upgrade instead of +1
- **Scythe** — attack rate roughly doubled (endCooldown 0.85s → 0.425s, burstTime 1.5s → 0.75s)

---

## Rarity & Toggle Changes

| Item | Change |
|------|--------|
| Bob's Lantern | Rare → Legendary |
| Golden Shield | Rare → Epic |
| Slurp Gloves | Now toggleable |
| Turbo Skates | → Rare, non-toggleable (always in pool) |
| Energy Core | Legendary → Rare |
| Electric Plug | Rare → Epic |
| Spiky Shield | Epic → Legendary, now toggleable, armor per stack 10 → 50 |
| Sucky Magnet | Legendary → Epic, non-toggleable (always in pool) |
| Scarf | Legendary → Rare, non-toggleable |
| Backpack | → Common |
| Cursed Doll | → Legendary, toggleable |
| Phantom Shroud | → Rare, toggleable |
| Beer | Now toggleable |
| Thunder Mitts | Now toggleable |
| Medkit / Slippery Ring / Oats / Golden Glove | Now toggleable |
| Echo Shard / Brass Knuckles / Idle Juice / Demonic Blood | Non-toggleable (always in pool) |
| Skuleg / Old Mask / Battery / Key | Non-toggleable (always in pool) |

---

## Stat Toggles (Chaos / Gamble pool)

Curate which stats can roll from Chaos rolls, Gamble Tome, Dicehead passive, and Charge Shrines. Open the toggle menu with **F3**. Choices persist via config and apply live mid-run.

| Stat | Default | Reason |
|------|---------|--------|
| HealthRegen | Off | Trivial defensive stat |
| Shield | Off | Trivial defensive stat |
| Thorns | Off | Trivial defensive stat |
| Armor | Off | Trivial defensive stat |
| Evasion | Off | Trivial defensive stat |
| ProjectileSpeedMultiplier | Off | Low impact |
| KnockbackMultiplier | Off | Low impact |
| Duration | On | Useful — toggle off to remove |
| Movement Speed | On | Useful — toggle off to remove |

Off = excluded from the pool by default (toggle on to re-enable). On = in the pool by default (toggle off to remove).

---

## Minimap Icons

- Chests colored by type (brown = normal, gold = free)
- Microwaves colored by rarity tier
- Cursed Shrines, Challenge Shrines, and Magnet Shrines have distinct colors
- Shady Guy colored by rarity

---

## Microwave Spawns

Default spawn count raised to 2 (max 3).

---

## Combat Scaling

- HP scaling per minute: 0.1 → 0.2
- Damage scaling per minute: 0.028 → 0.056
- Knockback resistance scaling: removed

---

## Character Stats

All characters normalized to minimum 1.2× speed, jump 10, pickup range 10. Characters with no starting shield receive +10 shield.

---

## Settings Tab

A native **"Community Patch"** tab is added to the game's Settings menu. From there you can:

- **Rebind every mod hotkey** — no config-file editing needed
- **SFX Volume sliders** — separate Weapon / Hit / Item sliders to tune or mute combat and item sounds independently of the game's master volume
  - *Note:* some on-hit item effects share the game's generic impact sound, so they're scaled by the **Hit** slider rather than Item
- **Mod Menu Opacity** — background opacity of the mod's own windows

---

## Tools & Automation

- **Map Scanner** (open with hotkey, start/stop with another) — pick how many map features you want, then it auto-rerolls the run until a map matches, stops, and pauses. Criteria: Moais, Shady Guys, combined Moai+Shady, Boss Curses (exact match), total Microwaves, and Microwaves by tier. Runs fully in-process — no external tool.
- **Scaling Auto-Upgrade** — auto-picks level-up choices using a bucket priority system: new weapons/tomes & legendaries first, then scaling tomes (XP / Difficulty), then luck / weapons / other tomes, weighted by rarity and endgame stat importance. XP stops being prioritised near its 10× cap; Difficulty (Cursed) stops once it hits the ~600 effective cap.
  - **Auto-Upgrade Log** — a separate window (newest-first) showing what it picked and what it skipped.
- **Smart Skip Chest Animation** — auto-enables chest-open skipping when you run out of banishes, turns it back off when you gain a Golden Ring, and resets after each run.
- **Priority Targeting** — replaces auto-aim target selection with a scorer that prioritises bosses/elites, then the nearest enemy you can kill soonest (avoids leaving low-HP stragglers alive).
- **Game Speed** — toggles between 1× and 2×. Counts as a cheat, so it blocks leaderboard submission for that run.
- **Effects Opacity** — toggles the game's Settings > Effects particle opacity between 0% and 100%.

---

## Controls

All hotkeys are rebindable in the **Community Patch** settings tab. Defaults:

| Key | Action |
|-----|--------|
| F1 | Toggle mod menu (password protected) |
| F2 | Toggle damage chart (death screen) |
| F3 | Toggle Chaos/Gamble stat-toggle menu |
| F4 | Open Map Scanner window |
| F5 | Start / stop the map scan |
| F6 | Toggle Smart Skip Chest Animation |
| F7 | Toggle Priority Targeting |
| F8 | Toggle Scaling Auto-Upgrade |
| F9 | Toggle Auto-Upgrade log window |
| F11 | Toggle Effects opacity (0% / 100%) |
| T | Toggle Game Speed (1× / 2×) |
| Left Ctrl | Fast fall (hold while airborne) |

---

## Discord

Bug reports and feedback: https://discord.gg/BsqCqsX63u

---

## Source

https://github.com/TurnipTheBeet/Megabonk---Community-Patch

---

## AI Disclosure

This mod was developed with AI assistance (Claude by Anthropic).