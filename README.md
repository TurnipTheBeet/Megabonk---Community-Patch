# Megabonk Community Patch

> **⚠ Work in Progress** — Actively developed. Expect bugs and frequent updates.

A community-driven balance and quality-of-life mod for Megabonk.

---

## Community Leaderboard

- Replaces the Steam leaderboard with a shared community leaderboard
- Scores submitted only when a new personal best is set — no redundant server calls
- Personal bests cached locally on main menu load; back-to-back runs work without re-querying
- Friends tab shows server data filtered by your Steam friends
- **Personal tab** — shows your best run per character (scroll with mouse wheel)
- Global/Friends tabs support mouse wheel scrolling with correct rank numbers
- Server rejects submissions from outdated mod versions
- Unauthorized mods detected at startup (BepInEx chainloader) — score submission blocked and warning banner shown
- Shows an in-game warning banner if your mod is out of date
- F1 menu features disable leaderboard for the run and show a warning banner

---

## Gameplay

- **Fast Fall** — hold Slide while airborne to fast fall
- **Full Heal** — heart powerup fully heals instead of partial
- **Pots** — drop a random powerup instead of always health
- **Greed Shrine** — also grants +5% XP and +5% luck
- **Microwaves** — Default spawn count raised to 2 (max 3)

---

## Character

- **Stats** — All characters normalized to minimum 1.2× speed, jump 10, pickup range 10. Characters with no starting shield receive +10 shield.
- **Tony McZooms** — Zap passive grants +0.25 projectiles per level
- **Roberto** — Hoarder passive also grants +0.5% elite damage per level

---

## Weapons

- **All Weapons** — +10% crit chance and +20% crit damage added to upgrade pool
- **Bow / Revolver** — +2 projectiles per level upgrade instead of +1
- **Bluetooth Dagger** — correctly triggers Lightning Orb stun (element fix)
- **Scythe** — attack rate roughly doubled (endCooldown 0.85s → 0.425s, burstTime 1.5s → 0.75s)

---

## Tome

- **Chaos Tome** — Some stats have been removed from the chaos tome stat pool - See below

---

## Items

- **Item Caps** — Anvil (max 2), Overpowered Lamp (max 3), Za Warudo (max 10)
- **Grandma's Secret Tonic** — baseRadius=4, radiusPerAmount=2, maxRadius=16m (max 6 stacks); pool slot removed once cap is reached
- **Spicy Meatball** — baseRadius=8, radiusPerAmount=4, maxRadius=32m (2× Grandma's values)
- **Brass Knuckles** — size cap removed
- **Cactus** — thorn range scales with player size; 4 projectiles per stack
- **Bob's Lantern** — fire rate doubled; rarity changed to Legendary
- **Backpack** — grants +2 projectiles per stack instead of +1
- **Cursed Doll** — 50% max HP damage (was 30%), curses 7 enemies per doll (was 2); rarity changed to Legendary
- **Green Credit Card** — chest price increase reduced from 10% to 2% per card
- **Echo Shard** — overflow chance rolls extra shards
- **Golden Ring** — grants +1 banish per ring held; drop chance increased from 1/400 to 1/128
- **Golden Shield** — removes reduced gold penalty on Kevin self-damage
- **Turbo Juice** (formerly Idle Juice) — renamed; now ramps damage while you keep **moving** instead of standing still (up to +100%)
- **Quin's Mask** — size cap: baseRadius=4, radiusPerAmount=2, maxRadius=16m (max 6 stacks); pool slot removed once cap is reached

---

## Bosses

- **BOMBUS** — giant Bee boss (15× scale). First appears 15 minutes into overtime, then every 5 minutes — multiple can be alive at once. Killable now (no longer invulnerable) but debuff-immune with massive HP; gets faster over time and the smaller it shrinks. Drops a **Corrupt chest** on death.
- **Corrupt Chest** — the game's unfinished evil chest now works: missing mesh/material restored (was rendering magenta) and it spawns on the ground. Opens to a **guaranteed Golden Ring** (the game's Corrupted-rarity loot pool is empty).

---

## Rarity & Toggle Changes

| Item | Change |
|------|--------|
| Bob's Lantern | Rare → Legendary |
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
| Echo Shard / Brass Knuckles / Turbo Juice / Demonic Blood | Non-toggleable (always in pool) |
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
- Custom Icons will be coming soon

---

## Combat Scaling

- HP scaling per minute: 0.1 → 0.2
- Damage scaling per minute: 0.028 → 0.056
- Knockback resistance scaling: removed

---

## Controls

| Key | Action |
|-----|--------|
| F1 | Toggle debug menu (password protected)(Disables leaderboard uploads) |
| F2 | Toggle damage chart (death screen) |
| Slide (airborne) | Fast fall |

---

## Support

If you enjoy the mod: https://ko-fi.com/pheeesh

---

## Discord

Bug reports and feedback: https://discord.gg/BsqCqsX63u

---

## Source

https://github.com/TurnipTheBeet/Megabonk---Community-Patch

---

## AI Disclosure

This mod was developed with AI assistance (Claude by Anthropic).
