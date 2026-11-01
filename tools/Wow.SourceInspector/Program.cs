using System.Text.Json;
using System.Text.Json.Serialization;
using Wow.Core.Content;

namespace Wow.SourceInspector;

internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintHelp();
            return 0;
        }

        if (!string.Equals(args[0], "inspect", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            PrintHelp();
            return 2;
        }

        var path = GetOption(args, "--path");
        if (string.IsNullOrWhiteSpace(path))
        {
            Console.Error.WriteLine("Missing required option: --path");
            PrintHelp();
            return 2;
        }

        if (!TryParseRole(GetOption(args, "--role"), out var role))
        {
            Console.Error.WriteLine(
                "Invalid --role. Use classic-world, wotlk-gameplay, midnight-characters, or omit it.");
            return 2;
        }

        var jsonPath = GetOption(args, "--json");

        try
        {
            var manifest = new SourceInstallationInspector().Inspect(path, role);
            PrintSummary(manifest);

            if (!string.IsNullOrWhiteSpace(jsonPath))
            {
                var fullOutputPath = Path.GetFullPath(jsonPath);
                var outputDirectory = Path.GetDirectoryName(fullOutputPath);
                if (!string.IsNullOrWhiteSpace(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var json = JsonSerializer.Serialize(manifest, JsonOptions);
                File.WriteAllText(fullOutputPath, json);
                Console.WriteLine();
                Console.WriteLine($"JSON manifest written to: {fullOutputPath}");
            }

            return 0;
        }
        catch (DirectoryNotFoundException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 2;
        }
        catch (IOException exception)
        {
            Console.Error.WriteLine($"I/O error: {exception.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException exception)
        {
            Console.Error.WriteLine($"Access denied: {exception.Message}");
            return 1;
        }
    }

    private static string? GetOption(IReadOnlyList<string> args, string option)
    {
        for (var index = 1; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    private static bool TryParseRole(string? value, out SourceRole role)
    {
        role = value?.ToLowerInvariant() switch
        {
            null or "" => SourceRole.Unspecified,
            "classic-world" => SourceRole.ClassicWorld,
            "wotlk-gameplay" => SourceRole.WotlkGameplay,
            "midnight-characters" => SourceRole.MidnightCharacters,
            _ => (SourceRole)(-1)
        };

        return Enum.IsDefined(role);
    }

    private static void PrintSummary(SourceInspectionManifest manifest)
    {
        Console.WriteLine("WoW Source Inspector");
        Console.WriteLine("===================");
        Console.WriteLine($"Root:       {manifest.RootPath}");
        Console.WriteLine($"Role:       {FormatRole(manifest.Role)}");
        Console.WriteLine($"Container:  {manifest.ContainerFormat}");
        Console.WriteLine($"Confidence: {manifest.DetectionConfidence}");

        if (manifest.ExecutableName is not null)
        {
            Console.WriteLine($"Executable: {manifest.ExecutableName}");
        }

        if (manifest.ExecutableVersion is not null)
        {
            Console.WriteLine($"Exe version: {manifest.ExecutableVersion}");
        }

        Console.WriteLine($"Build info: {manifest.HasBuildInfo}");
        Console.WriteLine($"CASC data:  {manifest.HasCascDataDirectory}");
        Console.WriteLine($"CASC index: {manifest.HasCascIndicesDirectory}");
        Console.WriteLine($"MPQ files:  {manifest.MpqArchives.Count}");

        if (manifest.MpqArchives.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("MPQ archives:");
            foreach (var archive in manifest.MpqArchives)
            {
                Console.WriteLine($"  {archive.RelativePath} ({FormatSize(archive.SizeBytes)})");
            }
        }

        if (manifest.Notes.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Notes:");
            foreach (var note in manifest.Notes)
            {
                Console.WriteLine($"  - {note}");
            }
        }
    }

    private static string FormatRole(SourceRole role) => role switch
    {
        SourceRole.ClassicWorld => "Classic world data",
        SourceRole.WotlkGameplay => "WotLK gameplay data",
        SourceRole.MidnightCharacters => "Midnight character data",
        _ => "Unspecified"
    };

    private static string FormatSize(long bytes)
    {
        const double KiB = 1024.0;
        const double MiB = KiB * 1024.0;
        const double GiB = MiB * 1024.0;

        return bytes switch
        {
            >= (long)GiB => $"{bytes / GiB:F2} GiB",
            >= (long)MiB => $"{bytes / MiB:F2} MiB",
            >= (long)KiB => $"{bytes / KiB:F2} KiB",
            _ => $"{bytes} B"
        };
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            WoW Source Inspector

            Usage:
              dotnet run --project tools/Wow.SourceInspector -- inspect --path <installation-directory> [options]

            Options:
              --role <role>   classic-world | wotlk-gameplay | midnight-characters
              --json <path>   Write a JSON inventory manifest to this local path
              --help          Show this help

            Examples:
              dotnet run --project tools/Wow.SourceInspector -- inspect --path "D:\Games\WoW-Classic" --role classic-world --json "source-manifests\classic.json"
              dotnet run --project tools/Wow.SourceInspector -- inspect --path "D:\Games\WoW-WotLK" --role wotlk-gameplay --json "source-manifests\wotlk.json"
              dotnet run --project tools/Wow.SourceInspector -- inspect --path "D:\Games\World of Warcraft" --role midnight-characters --json "source-manifests\midnight.json"

            This tool records local installation metadata only. It does not open, extract,
            modify, upload, or redistribute archive payloads.
            """);
    }
}
