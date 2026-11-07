# Vanilla Expanded collection 1884025115 — preliminary co-op compatibility audit

Audit date: 2026-11-07. Target: RimWorld **1.7**, Multiplayer Resync fork,
one shared colony, Steam host/client. Collection:
https://steamcommunity.com/sharedfiles/filedetails/?id=1884025115

**Scope & uncertainty:** the collection was indexed as **116 items**; the
Steam page intermittently returned HTTP 429, preventing a fresh machine-readable
snapshot of every Workshop item ID and version. This is a **first-pass triage**
of identified items and mod families, **not a verified 116-row compatibility
manifest**. A Steam collection is not the same as the active ModsConfig.xml.
A mod being updated for 1.7 does not establish multiplayer compatibility.

## Explicit exclusions for first co-op test

| Workshop item | Decision | Grounded reason |
|---|---|---|
| [Vanilla Factions Expanded - Mechanoids](https://steamcommunity.com/sharedfiles/filedetails/?id=2329011599) | Exclude | Creator explicitly says the original mod will not be updated even to 1.6; 1.7 is not supported |
| [Vanilla Storytellers Expanded - Winston Waves](https://steamcommunity.com/sharedfiles/filedetails/?id=3215569151) | Exclude | Mod author calls Multiplayer incompatible; gravship gameplay is also explicitly unsupported |

Important: excluding VFE Mechanoids **does not mean removing Biotech** or
RimWorld's vanilla mechanoids.

## Quarantine until targeted patches and staged tests

These mods are not proven permanently incompatible. Leave off the first test
save, then add in controlled batches.

| Module | Reason to defer | Patch/testing priority |
|---|---|---|
| [Vehicle Framework](https://github.com/SmashPhil/Vehicle-Framework), [Vanilla Vehicles Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877), Vanilla Vehicles Expanded - Tier 3 | Vehicle framework async pathfinding can diverge across clients; caravan UI and routing have compatibility reports | P1 targeted |
| [Vanilla Gravship Expanded - Chapter 1](https://steamcommunity.com/sharedfiles/filedetails/?id=3609835606) + [Chapter 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3799737423) | Map transfers, orbital/ship tick paths, launch and landing; incompatible with other gravship overhauls per author | P2, both chapters as a pair |
| Vanilla Trading Expanded | Dynamic trading decisions, UI, world-wide state changes | P1 |
| [Vanilla Outposts Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2688941031) | World object/gizmo actions, world-layer pawns and caravans | P1 |
| Vanilla Psycasts Expanded + Hemosage/Puppeteer | Ability casting, selection dialogs, transient targeting, potential unsynced UI/RNG | P1 |
| Vanilla Genetics Expanded | Bio-engineering state and pawn/Thing generation | P1 |
| Vanilla Factions Expanded - Insectoids 2 | New pawn-like insects, hive and boss logic, special map systems | P1 |
| Vanilla Factions Expanded - Empire + Deserters | Quests, diplomacy, factions and world actions | P2 |
| Vanilla Factions Expanded - Classical, Tribals, Medieval 2 | Special incident/world/scenario/game-mode mechanics | P2 |
| Vanilla Quests Expanded - Ancients, Drone Factory, Cryptoforge, Deadlife | Special quests, persistent effects, generated encounters | P2 |
| Vanilla Temperature Expanded | Custom environment and temperature tick code | P2 |
| Vanilla Furniture Expanded - Factory | Automated production and UI controls; needs gameplay action testing | P2 |
| Vanilla Social Interactions Expanded | New pawn interactions, schedules, interface decisions | P2 |
| Vanilla Ideology Expanded - Memes and Structures / Dryads | Special ideology rituals, pawns and gameplay conditions | P2 |
| Vanilla Races Expanded (Android, Archon, Fungoid, Highmate, Lycanthrope, Sanguophage, etc.) | Custom genes/needs, hediffs, pawn logic; test independently | P2 |
| Vanilla Events Expanded, event storytellers | Incidents, storyteller incidents, world generation, rewards | P2 |

## First test cohort (low-risk candidates; **not** guaranteed compatible)

Vanilla Expanded Framework (mandatory shared dependency), Vanilla Textures
Expanded, Vanilla Hair Expanded, Vanilla Apparel Expanded, Vanilla Armour
Expanded, Vanilla Weapons Expanded and its predominantly equipment-only
submodules, Vanilla Furniture Expanded modules adding ordinary definitions,
Vanilla Plants Expanded (base, Flowers, Succulents), Vanilla Cooking Expanded
(base, Sushi, Stews), Vanilla Brewing Expanded, Vanilla Fishing Expanded,
Vanilla Books Expanded, and ordinary animal definitions.

Start with approximately 15–25 modules after RimSort resolves dependencies.
Add independent content modules in batches; if a module adds settings/actions,
exercise those actions separately.

## 1.7 support is not multiplayer support

- [VE Framework](https://steamcommunity.com/workshop/filedetails?id=2023507013) lists 1.7 and was updated October 21.
- [Psycasts Expanded](https://github.com/Vanilla-Expanded/VanillaPsycastsExpanded/releases) v3.01 (October 15) was compiled against 1.7; casting still needs multiplayer tests.
- [Vanilla Races Expanded - Pigskin](https://steamcommunity.com/workshop/filedetails/?id=3164798809) lists 1.7 after its October 14 update. Do not ban it based only on October 12 user reports.
- [VFE - Insectoids 2](https://steamcommunity.com/sharedfiles/filedetails/?id=3309003431) lists 1.7; it is a high-value stress test rather than a confirmed blocker.
- [Vanilla Genetics Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2801160906) lists 1.7, updated October 18.
- [Multiplayer-Compatibility](https://github.com/rwmt/Multiplayer-Compatibility)
  has a [1.7 port PR #633](https://github.com/rwmt/Multiplayer-Compatibility/pull/633),
  but that PR is draft and has not been validated with a host/client session.
  **Do not assume the Workshop build supports our .NET 10 fork.**
- Vehicle Framework risk is supported by [community desync patch](https://rimmods.com/mods/vehicle-framework-multiplayer-desync-fix)
  and [Vehicle compat PRs](https://github.com/rwmt/Multiplayer-Compatibility/pulls).
- Gravship Chapter 2 has compatibility work in
  [PR #627](https://github.com/rwmt/Multiplayer-Compatibility/pull/627).
- Author of Winston Waves lists Multiplayer and gravships as incompatible:
  https://steamcommunity.com/sharedfiles/filedetails/?id=3215569151 .

## Deterministic test plan

1. **Baseline:** fork + Harmony 3 + identical game/DLC versions + VEF.
   Two players, 30 minutes, 2 saves/reloads, reconnect. Back up save.
2. **Definitions:** add simple equipment/texture/furniture/cooking modules
   in batches of 5–10; run real game actions, not just a menu boot.
3. **Pawns:** genes, psycasts, social actions, genetics and races
   separately. Both players use each important gizmo/ability. Verify
   synchronized mod settings, pawn health, inventories, abilities.
4. **World/map:** factions, outposts, quests, caravans, new maps,
   Insectoids 2 bosses/raids and special events.
5. **Heavy systems:** Vehicle Framework driving/caravans/pathing;
   gravship launch/landing/orbit with 1 and then 2 chapters.
6. **Recovery:** mid-session join, disconnected-client return,
   save/load, multiple maps, 2x/3x speed and late colony TPS.

For each regression collect per-client game version, workshop IDs and their
load order, ModsConfig.xml, Player.log, MpDesyncs ZIPs, exact scenario actions,
the first divergent tick reported, and the commit SHA of this fork.
In RimWorld 1.7, Player.log generally lives in the `Logs` subfolder.
Test on a disposable save.

The fork's optional map fingerprints compare **only** spawned Thing ID/def,
map position, stack and HP; they do not detect every mod state discrepancy.
Misidentification of cause is possible. Never patch an unknown mod solely
because it appears in a desync stack trace.

## Needed to produce an exact exclusion config

Obtain a machine-readable snapshot of the collection's **116 Workshop IDs**
and the *actual* sorted `ModsConfig.xml` after subscription (some items may be
missing or replaced by successor mods). Check every hard-excluded item by
Workshop ID, plus exact version and DLC dependencies. Until then, all
non-verified modules remain candidates, not a definitive 116-item support claim.
