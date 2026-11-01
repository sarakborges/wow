# Runtime architecture

## Product definition

A standalone, single-player RPG runtime that combines three independent content sources:

| Concern | Canonical source |
|---|---|
| Gameplay rules | WotLK 3.3.5a, build 12340 |
| Character visuals | Midnight 12.x; exact build to be pinned after inspection |
| World and initial environment | Classic-era data; exact build to be pinned after inspection |

The source versions are not interchangeable runtime versions. Each source is handled by an adapter and converted into project-owned runtime contracts.

## Runtime layers

1. **Client / presentation (Godot):** scene lifecycle, rendering, camera, input, animation playback, audio and UI.
2. **Game simulation (C#):** player state, class resources, spell execution, cooldowns, auras, talents, combat resolution, NPC brains, quests, loot and progression.
3. **World simulation (C#):** world-cell residency, collision, navigation, triggers, time and environment state.
4. **Content domain (engine-independent C#):** stable IDs, normalized definitions, source provenance and schema versions.
5. **Import tools:** offline readers, validators and converters for each source generation.

The simulation must be testable without loading a Godot scene. Presentation consumes simulation state and sends player intents; it must not own combat rules.

## Source adapters

- **WotLK adapter:** imports the spell, talent, class, item and creature definitions required by the 3.3.5a ruleset. Spell effects are represented as typed runtime operations rather than executable client scripts.
- **Classic world adapter:** reads world/map metadata and terrain/object sources, then emits normalized world tiles and placement records.
- **Midnight character adapter:** reads modern character model, skeleton, skin, animation and customization data, then emits a normalized character asset bundle.

Adapters do not leak source-specific IDs or binary structures into gameplay or rendering systems. Keep original IDs in provenance and maintain explicit source-to-runtime mappings.

## Local game state

All player and world state is local. There is no account service, login flow, remote authority, MMO protocol or requirement for multiple concurrent players.

Save files should contain runtime schema versions and stable runtime IDs, not raw pointers into extracted files. Imported asset manifests are separate from save data.

## First vertical slice

The first meaningful content test is deliberately narrow:

- one Classic terrain tile (including height, texture layers and at least one placed object);
- one Midnight character model with a compatible skeleton and at least one locomotion animation;
- a third-person camera and local character movement;
- a reproducible import manifest recording source build, source identifier, source hash, importer version and runtime schema version.

A placeholder scene is already committed to test the engine and input loop. It is not evidence that any WoW format is supported yet.
