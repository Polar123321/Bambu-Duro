using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ConsoleApp4.Helpers;






internal static class AppSettingsFile
{
    internal const string FileName = "appsettings.json";

    internal static string ResolvePath()
    {
        
        var baseDirPath = Path.Combine(AppContext.BaseDirectory, FileName);
        if (File.Exists(baseDirPath))
        {
            return baseDirPath;
        }

        
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

