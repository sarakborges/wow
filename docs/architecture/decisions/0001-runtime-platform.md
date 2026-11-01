# ADR 0001: Godot .NET as the initial client runtime

- Status: accepted for the prototype; subject to a technical spike before production-scale import.
- Date: 2026-10-31.

## Decision

Use Godot 4.7.2 .NET with C# for the first client prototype. Keep reusable domain and simulation code in an engine-independent .NET library.

## Reasons

- The project needs a custom third-person 3D client and a fully custom UI.
- Godot supplies scene management, rendering, physics, animation, resource loading and editor tooling.
- C# allows simulation and import code to live outside scene scripts and be tested independently.
- The project owner already has practical experience with Godot and C#.

## Constraints and validation

- Pin the engine SDK version in the client project file.
- The first placeholder scene is an engine/input smoke test, not a renderer benchmark.
- Compare Godot Forward+ and Compatibility renderers with representative terrain, WMO-style geometry, character skinning and effects before selecting the long-term renderer.
- Keep the core library free of Godot references.
- Revisit the engine decision if large-world streaming, custom material behavior, model animation or rendering performance cannot meet the prototype requirements.

## Consequences

The client project and engine-independent core are separate projects. Godot-specific scene nodes are adapters over simulation and content contracts, not the source of gameplay truth.
