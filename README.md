# WoW Single-player Runtime

A custom single-player game runtime combining:

- **WotLK 3.3.5a (build 12340)** as the gameplay ruleset: classes, spells, talents, glyphs, combat and progression.
- **Midnight (12.x)** as a visual-content source: character models, customization, skeletons and animations. The exact source build will be pinned after an asset audit.
- **Classic-era data** as the initial world-content source: maps, terrain, towns and dungeons. The exact client/data build will be pinned after an asset audit.

This is a standalone, local game runtime—not a private-server fork. It does not implement the original WoW network protocol. Gameplay simulation is local, and simulation data is kept separate from presentation and source-file formats.

## Current status

**Bootstrap prototype committed.** The Godot .NET project opens a generated test scene with a placeholder third-person character, a flat test floor, camera, lighting and WASD movement. It does not yet load WoW maps, models, animations, spells or talents.

The first content milestone is a vertical slice that loads one Classic terrain area and one Midnight character model into the same runtime, renders and animates the character, and supports local movement. This must validate the cross-version asset pipeline before gameplay implementation expands.

## Repository layout

- `docs/architecture/` — runtime boundaries, asset pipeline and technical decisions.
- `docs/research/` — source-version and format-support investigation.
- `docs/roadmap/` — milestones and acceptance criteria.
- `src/Wow.Client/` — Godot client and prototype scene.
- `src/Wow.Core/` — engine-independent C# domain and import contracts.
- `tools/` — planned asset inspection, conversion and validation utilities.
- `tests/` — planned parser, adapter and runtime-contract tests.
- `assets/` — project-authored assets and local import outputs; proprietary source data is excluded.

## Technology

- Godot **4.7.2 .NET** for the client.
- C# / .NET 8 for the initial engine-independent core library.
- Source adapters and offline import tools for each WoW data generation.

The Godot SDK version is pinned in `src/Wow.Client/Wow.Client.csproj`.

## Development principles

1. Keep gameplay rules independent from graphics and imported assets.
2. Treat each WoW generation as a source format, not as the runtime's internal schema.
3. Normalize supported source data into documented, versioned project-owned contracts.
4. Prefer read-only import and provenance tracking; never modify source game installations.
5. Never commit proprietary client data, extracted game assets, copyrighted UI artwork, or generated exports derived from them.
6. Preserve source attribution and license notices for all third-party code and tools.
7. Do not copy code from repositories without a compatible license.

## Legal and asset boundary

This repository contains original code, documentation, schemas and project-authored content only. Local game data is not included. Any inspection, extraction, conversion or use of game data must be independently reviewed for compliance with the relevant terms and applicable law. The project does not distribute Blizzard game files, extracted assets, or a client executable.
