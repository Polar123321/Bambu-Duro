using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ConsoleApp4.Helpers;

/// <summary>
/// Resolves, loads and saves the active appsettings file used at runtime.
/// Why: WinForms apps frequently run with a working directory different from the output folder,
/// which can cause the UI to read/write a different file than the host builder consumes.
/// </summary>
internal static class AppSettingsFile
{
    internal const string FileName = "appsettings.json";

    internal static string ResolvePath()
    {
        // Prefer the output folder copy (copied by the .csproj) to keep UI and host consistent.
        var baseDirPath = Path.Combine(AppContext.BaseDirectory, FileName);
        if (File.Exists(baseDirPath))
        {
            return baseDirPath;
        }

        // Fallback for dev scenarios where the working directory is the project folder.
        return Path.Combine(Directory.GetCurrentDirectory(), FileName);
    }

    internal static JsonNode? Load(string path, out Exception? error)
    {
        error = null;
        try
        {
            if (!File.Exists(path))
            {
                error = new FileNotFoundException($"Settings file not found: {path}", path);
                return null;
            }

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonNode.Parse(json);
        }
        catch (Exception ex)
        {
            error = ex;
            return null;
        }
    }

    internal static bool Save(string path, JsonNode root, out Exception? error)
    {
        error = null;
        try
        {
            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            // Atomic-ish write: write a temp file beside the target then rename.
            // Why: avoids corrupting the settings if the process crashes mid-write.
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            return false;
        }
    }
}

