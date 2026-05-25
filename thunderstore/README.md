# Megabonk Community Patch

> **⚠ Work in Progress** — Actively developed. Expect bugs and frequent updates.

A community-driven balance and quality-of-life mod for Megabonk.

---

## Community Leaderboard

- Replaces the Steam leaderboard with a shared community leaderboard
- Scores submitted automatically at the end of each run
- Friends tab shows server data filtered by your Steam friends
- Server rejects submissions from outdated mod versions
- Shows an in-game warning banner if your mod is out of date

---

## Gameplay

- **Fast Fall** — hold Slide while airborne to fast fall
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
- **Golden Shield** — removes reduced gold penalty on Kevin self-damage
- **Bluetooth Dagger** — correctly triggers Lightning Orb stun (element fix)
- **All Weapons** — +10% crit chance and +20% crit damage added to upgrade pool
- **Bow / Revolver** — +2 projectiles per level upgrade instead of +1

---

## Rarity & Toggle Changes

| Item | Change |
|------|--------|
| Bob's Lantern | Rare → Legendary |
| Energy Core | Rare → Epic |
| Electric Plug | Rare → Epic |
| Spiky Shield | Epic → Rare, now toggleable |
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

## Stat Blacklists

Excludes broken/junk stats from Chaos rolls, Gamble Tome, Dicehead passive, and Charge Shrines. Hardcoded — not user-configurable.

| Stat | Reason |
|------|--------|
| HealthRegen | Trivial defensive stat |
| Shield | Trivial defensive stat |
| Thorns | Trivial defensive stat |
| Armor | Trivial defensive stat |
| Evasion | Trivial defensive stat |
| DurationMultiplier | Low impact |
| ProjectileSpeedMultiplier | Low impact |
| KnockbackMultiplier | Low impact |
| MoveSpeedMultiplier | Low impact |

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

## Controls

| Key | Action |
|-----|--------|
| F1 | Toggle debug menu (password protected) |
| F2 | Toggle damage chart (death screen) |
| Slide (airborne) | Fast fall |

---

## Discord

Bug reports and feedback: https://discord.gg/BsqCqsX63u

---

## Source

https://github.com/TurnipTheBeet/Megabonk---Community-Patch
