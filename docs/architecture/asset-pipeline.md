# Asset and data pipeline

## Import flow

```text
Local source installation / permitted source files
        |
        v
Read-only source reader
        |
        v
Version-specific parser
        |
        v
Validation + provenance capture
        |
        v
Project-owned normalized representation
        |
        +--> Runtime content catalog (JSON / binary metadata)
        +--> Godot-importable assets (for example glTF and supported textures)
        +--> Import report and validation diagnostics
```

Source installations are inputs only. Importing must never patch, rename or rewrite source files.

## Three independent pipelines

### WotLK gameplay data

The canonical gameplay ruleset is WotLK 3.3.5a, build 12340. Initial coverage should include class/spec metadata, spell records, spell effects, ranks, talent trees/ranks, glyphs, cooldowns, auras and required supporting records.

A spell record is not sufficient to reproduce spell behavior. The importer must preserve effect indices, target selectors, trigger relationships, aura types, power costs, range, duration and rank relationships where present. Runtime behavior is implemented as typed operations and validated against known examples.

### Classic world data

The world pipeline should be investigated in this order:

1. map and tile discovery;
2. terrain geometry and height data;
3. terrain texture layers and alpha maps;
4. world model placement and doodads;
5. liquids, collision and area metadata;
6. navigation and streaming boundaries.

Legacy formats commonly encountered in this lane include WDT, ADT, WDL, WMO, M2 and BLP, with archive/container handling depending on the chosen Classic build. Do not assume every client revision has identical chunk layouts.

### Midnight character data

The character pipeline needs to identify the exact modern build before implementation. Audit model, skin, skeleton, animation, texture, attachment and customization data independently. Modern CASC/FileDataID and DB2 access may be involved. Do not assume a modern M2/skin/skeleton bundle can be loaded by a legacy parser.

The first character test should prove mesh + skeleton + skinning + one animation before adding customization or equipment attachment complexity.

## Normalized runtime representation

- Use project-owned stable runtime IDs.
- Keep source build, source identifier, source hash, parser version and conversion version in provenance.
- Version every normalized schema.
- Keep source-specific data in import metadata; do not make runtime systems depend on raw DBC/DB2 row layouts or binary chunks.
- Prefer glTF for interchange where it preserves required skeleton and animation semantics; write adapters/tests for any WoW-specific behavior glTF cannot represent.
- Keep unconverted source data and generated imports outside version control.

## Tooling policy

Use existing tools as external inspection/export utilities where their licenses permit. Do not copy implementation code from a repository unless its license is present and compatible. The initial research candidate is [wow.export](https://github.com/Kruithne/wow.export), which advertises Retail/Classic browsing, model preview/export, terrain export and DB2 inspection. Its output still needs automated validation against this project's runtime requirements.
