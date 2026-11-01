# Source-version and format research

This is a working compatibility matrix, not a claim that all listed formats are already implemented.

| Content area | Target source | Initial investigation | Status |
|---|---|---|---|
| Classes, spells, talents, glyphs | WotLK 3.3.5a, build 12340 | DBC records, ranks, spell effects and reference implementations | Not implemented |
| World maps and terrain | Classic-era client data | WDT/ADT/WDL layout, tile stitching, terrain layers and liquids | Build to pin; not implemented |
| Character models | Midnight 12.x | Modern model, skin, skeleton, animation and texture bundle | Exact build to pin; not implemented |
| UI | Original project implementation | Recreate interaction and layout; do not copy proprietary artwork | Not implemented |
| Runtime | Godot 4.7.2 .NET | Third-person prototype, streaming and renderer spike | Bootstrap scene committed |

## Research candidates

- [wow.export](https://github.com/Kruithne/wow.export) — external inspection/export tool. Its README advertises Retail and Classic clients, M2/WMO preview, model export, terrain export and DB2 browsing. Validate each desired feature and output format against the exact source build.
- [AzerothCore](https://github.com/azerothcore/azerothcore-wotlk) — useful WotLK 3.3.5a server-side behavioral reference. Its repository is GPL-2.0. Treat its implementation as a reference unless the project explicitly adopts compatible licensing; do not copy code casually.
- [wowdev.wiki](https://wowdev.wiki/) — community format documentation. Verify version-specific layouts with actual files and independent implementations.
- [Godot documentation](https://docs.godotengine.org/en/stable/) — engine API and release information.

## Questions to resolve before writing production parsers

1. Which exact Classic client/data build will be the world source?
2. Which exact Midnight 12.x build will be the character source?
3. Which required model features survive export to glTF, and which need a custom sidecar/runtime component?
4. Which WotLK data source is available for spell/talent extraction, and what are the permitted local-use boundaries?
5. Which renderer and streaming strategy can handle the selected source assets at target quality?

No source game files or extracted assets belong in this repository.
