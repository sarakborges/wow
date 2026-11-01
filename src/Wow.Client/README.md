# Running the client prototype

## Required tools

1. **Godot 4.7.2 .NET** (the .NET-enabled editor build, not the standard/GDScript-only build).
2. **64-bit .NET 8 SDK** or newer.
3. Windows, Linux or macOS matching the downloaded Godot editor architecture.

Godot's C# support requires both the .NET-enabled editor and a separately installed .NET SDK. Installing only the normal Godot editor or only the .NET runtime is not sufficient.

## Open the correct project

Open this directory as the Godot project root:

`src/Wow.Client/project.godot`

Do not open the repository root. The `res://` paths are relative to this directory.

The project declares its C# feature and assembly name in `project.godot`. Those settings must match the `Wow.Client` assembly produced by `Wow.Client.csproj`.

## Build

In Godot, use the **Build** action in the editor toolbar before running the scene.

Alternatively, from this directory:

```sh
dotnet build Wow.Client.sln
```

The solution includes both the Godot client and the engine-independent core library.

## If Godot says the C# class cannot be found

The message often means the C# assembly has not been built or loaded; it does not necessarily mean the script path is wrong.

1. Confirm that the editor title/about dialog identifies the **.NET** build of Godot 4.7.2.
2. Confirm that `dotnet --list-sdks` shows an installed 8.x SDK (or newer).
3. Confirm that you opened `src/Wow.Client/project.godot`, not the repository root.
4. Click **Build** in the Godot editor. Resolve the first C# compiler or NuGet restore error before running the scene.
5. If the project was previously opened with a different Godot/.NET version, close Godot, remove the generated `.godot/mono/` directory, reopen the project with Godot 4.7.2 .NET, and build again.

The project ignores `.godot/`, `bin/` and `obj/`; these are regenerated locally and should not be committed.

## Current prototype

The main scene generates a test floor and placeholder capsule at runtime. WASD moves the capsule and Space jumps. This only validates the Godot C# boot path; it does not yet load WoW game data.
