# Import and inspection tools

## Available

- **Wow.SourceInspector** — read-only CLI that inventories local installation markers, MPQ archive metadata, CASC directory structure and root executable version metadata. See [tool usage](Wow.SourceInspector/README.md).

## Planned

- source inventory and build verification;
- archive/container and index inspection;
- Classic world tile inspection;
- Midnight character bundle inspection;
- WotLK gameplay data extraction/normalization;
- normalized asset validation and provenance manifest generation.

Import tools must be read-only against source installations. Keep parser implementations versioned and testable. Do not copy third-party code without a compatible license.
