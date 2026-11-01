# WoW Source Inspector

A small read-only CLI for recording the structure of local World of Warcraft installations before implementing version-specific importers.

## Build

From the repository root:

```sh
dotnet build src/Wow.Client/Wow.Client.sln
```

## Inspect an installation

```sh
dotnet run --project tools/Wow.SourceInspector -- inspect --path "<installation-root>" --role classic-world --json "<output-manifest.json>"
```

Supported roles:

- `classic-world`
- `wotlk-gameplay`
- `midnight-characters`

The role is a label supplied by the user. The tool does **not** infer or certify a specific expansion build from a container marker.

## Detection behavior

- **CASC:** detects `.build.info` and/or a `Data/data` / `Data/indices` layout.
- **MPQ:** finds `.mpq` archives in the root and a bounded directory depth.
- **Executable metadata:** attempts to read the version resource of a root-level WoW executable.
- **Unknown:** reports that no known marker was found.

The scanner deliberately does not recursively enumerate CASC's `Data/data` payload directory, which may contain a very large number of files. It records marker paths and MPQ archive names/sizes only.

## Output

The JSON file is a local inventory manifest with schema version, selected role, detected container type, detection confidence, markers, executable version (when available), MPQ archive paths/sizes, and diagnostic notes.

The output may contain local absolute paths. Keep manifests private if they reveal local usernames or installation layout. Do not commit installation manifests to this repository.

## Limitations

This is a discovery tool, not a parser. It does not inspect MPQ/CASC contents, resolve FileDataIDs, parse DBC/DB2, or establish exact client compatibility. Those require the next version-specific import stages.
