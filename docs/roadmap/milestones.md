# Milestones

## M0 — Repository and runtime bootstrap

**Status: initial bootstrap committed; local build/run still needs verification.**

- [x] Establish project boundaries and source-of-truth versions.
- [x] Create Godot .NET client project and engine-independent core library.
- [x] Add generated placeholder floor, capsule character, camera, lighting and keyboard movement.
- [ ] Open with Godot 4.7.2 .NET and verify import/build/run on the development machine.
- [ ] Add CI or a documented local verification script once the target environment is known.

Acceptance: the placeholder scene runs without any installed WoW client or imported game assets.

## M1 — Source inventory and import spike

- [ ] Record the exact Classic and Midnight source builds.
- [ ] Verify local source paths and read-only access.
- [ ] Create a CLI that lists files/records and reports source IDs and versions.
- [ ] Validate one export path for Classic terrain and one for a Midnight character.
- [ ] Capture provenance manifests and fail on incomplete/mismatched bundles.

Acceptance: one terrain tile and one animated character can be imported reproducibly, with source provenance and no modifications to the source installation.

## M2 — First cross-version world slice

- [ ] Render imported Classic terrain with texture layers and height.
- [ ] Render an imported Midnight character using its skeleton and skinning.
- [ ] Play idle and locomotion animations.
- [ ] Add ground collision and character movement over the imported tile.
- [ ] Validate coordinate scale, axes, origin, bounds and tile seams.

Acceptance: the character can move around a real Classic terrain tile, with no placeholder geometry required for the terrain or character.

## M3 — WotLK gameplay foundation

- [ ] Normalize the WotLK class/spec, spell and talent datasets needed for one class.
- [ ] Implement player level, resources, known spells, talent points and action bar bindings.
- [ ] Implement one target dummy with health and combat state.
- [ ] Implement one direct-damage spell, one resource-cost spell and one aura/buff.
- [ ] Add deterministic tests for cooldown, resource, target and damage calculations.

Acceptance: one WotLK class can fight a target dummy in the imported world using real WotLK spell/talent definitions.

## M4 — Playable single-player loop

- [ ] NPC interaction and basic AI.
- [ ] Loot and inventory.
- [ ] Quest objectives and reward flow.
- [ ] Character progression and talent respec rules.
- [ ] Local save/load with schema versioning.
- [ ] UI recreation for core HUD, spellbook, talents, inventory, character panel and quest log.

Acceptance: a small quest loop can be completed, saved and loaded without a network connection.

## M5 — Content expansion

Expand supported races, classes, maps, creatures, equipment, dungeons, quests and UI only after the corresponding import and runtime contracts are stable.
