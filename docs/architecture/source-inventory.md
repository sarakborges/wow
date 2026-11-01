# Local source inventory

## Purpose

Before writing binary parsers or converters, record which source installations are available and how they are packaged. The inventory tool is intentionally read-only and does not require any proprietary source data in the repository.

## Run

From the repository root:

```sh
dotnet run --project tools/Wow.SourceInspector -- inspect --path "<installation-root>" --role <role> --json "<local-output.json>"
```

Use one inventory per source role:

- `classic-world` — the selected Classic-era world source.
- `wotlk-gameplay` — the WotLK 3.3.5a gameplay source.
- `midnight-characters` — the selected Midnight character source.

The role is descriptive user input, not automatic version detection. Record the exact client build separately after verifying executable metadata and/or build manifests.

## Detection rules

| Marker | Interpretation |
|---|---|
| `.build.info` | Strong indicator of a CASC installation |
| `Data/data/` | CASC payload directory |
| `Data/indices/` | CASC index directory |
| `*.mpq` | MPQ archives; archive payloads are not inspected |
| Root-level WoW executable version | Optional executable metadata, when the platform exposes a version resource |

CASC payload directories are not recursively enumerated. MPQ archives are discovered only to a bounded directory depth. Symlink/reparse-point directories are skipped.

## Data handling

- Scans are local and read-only.
- No archive is opened, extracted, modified or uploaded.
- JSON manifests may contain absolute local paths. Keep them outside the repository unless they have been sanitized.
- Never commit original game data, extracted proprietary assets, or manifests that expose personal local paths.
- A marker match is not proof that an installation is the required game build.
