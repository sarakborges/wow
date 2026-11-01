# WoW Single-player Runtime

A custom single-player game runtime combining:

- **WotLK 3.3.5a** as the gameplay ruleset (classes, spells, talents, combat and progression).
- **Midnight** as a visual-content reference (character models, customization, skeletons and animations).
- **Classic** as the initial world-content reference (maps, terrain, towns and dungeons).

The project is not a private-server fork and does not aim to implement the original WoW network protocol. Gameplay simulation runs locally; presentation and simulation remain separate.

## Project status

**Stage: architecture and asset-pipeline investigation.** No playable client or imported game assets exist yet.

The first technical milestone is a small vertical slice that can load one Classic terrain area and one independently sourced character model into the same runtime, render and animate the character, and support basic local movement. This milestone must validate the data pipeline before gameplay implementation expands.

## Repository layout

- `docs/architecture/` — runtime boundaries, data model and technical decisions.
- `docs/research/` — format support, source evaluation and compatibility notes.
- `docs/roadmap/` — milestones and acceptance criteria.
- `src/` — game runtime and reusable libraries.
- `tools/` — asset inspection, conversion and validation utilities.
- `tests/` — automated tests for parsers, adapters and runtime contracts.
- `assets/` — project-authored assets and local import outputs; original game data is not committed.

See [Architecture](docs/architecture/overview.md) and [Roadmap](docs/roadmap/milestones.md).

## Development principles

1. Keep gameplay rules independent from graphics and imported assets.
2. Treat each WoW generation as a source format, not as the runtime's internal schema.
3. Normalize supported source data into documented, versioned project-owned contracts.
4. Prefer read-only import and provenance tracking; never modify source game installations.
5. Never commit proprietary client data, extracted game assets, copyrighted UI artwork, or generated exports derived from them.
6. Preserve source attribution and license notices for all third-party code and tools.
7. Do not copy code from repositories without a compatible license.

## Planned technology

The initial implementation is planned in **Godot .NET / C#**, subject to the renderer and asset-pipeline spike. The engine version will be pinned when the prototype project is created, after testing the required import and rendering capabilities.

## Legal and asset boundary

This repository contains original code, documentation, schemas and project-authored content only. Users must provide and lawfully possess any local game data they inspect. The project does not distribute Blizzard game files, extracted assets, or a client executable. Asset extraction and use must comply with applicable terms and law.
