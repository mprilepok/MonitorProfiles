using System.Text.Json;
using MonitorProfiles.Models;

namespace MonitorProfiles;

public static class ProfileManager
{
    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MonitorProfiles");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string GetProfilePath(string name) =>
        Path.Combine(ProfileDir, $"{name}.json");

    public static void SaveProfile(string name)
    {
        Directory.CreateDirectory(ProfileDir);

        var displays = DisplayManager.GetCurrentDisplays();
        if (displays.Count == 0)
        {
            Console.WriteLine("Error: No displays detected.");
            return;
        }

        var profile = new DisplayProfile
        {
            Name = name,
            SavedAt = DateTime.Now,
            Displays = displays
        };

        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(GetProfilePath(name), json);
        Console.WriteLine($"Profile '{name}' saved with {displays.Count} display(s).");
    }

    public static void LoadProfile(string name)
    {
        var path = GetProfilePath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Profile '{name}' not found.");
            return;
        }

        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<DisplayProfile>(json, JsonOptions);
        if (profile == null || profile.Displays.Count == 0)
        {
            Console.WriteLine($"Error: Profile '{name}' is empty or corrupt.");
            return;
        }

        Console.WriteLine($"Applying profile '{name}' ({profile.Displays.Count} display(s))...");
        if (DisplayManager.ApplyProfile(profile))
            Console.WriteLine("Profile applied successfully.");
        else
            Console.WriteLine("Profile applied with warnings.");
    }

    public static void DeleteProfile(string name)
    {
        var path = GetProfilePath(name);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Error: Profile '{name}' not found.");
            return;
        }

        File.Delete(path);
        Console.WriteLine($"Profile '{name}' deleted.");
    }

    public static List<string> GetProfileNames()
    {
        if (!Directory.Exists(ProfileDir))
            return [];

        return Directory.GetFiles(ProfileDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(n => n != null)
            .Cast<string>()
            .OrderBy(n => n)
            .ToList();
    }

    public static void ListProfiles()
    {
        if (!Directory.Exists(ProfileDir))
        {
            Console.WriteLine("No saved profiles.");
            return;
        }

        var files = Directory.GetFiles(ProfileDir, "*.json");
        if (files.Length == 0)
        {
            Console.WriteLine("No saved profiles.");
            return;
        }

        Console.WriteLine("Saved profiles:");
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<DisplayProfile>(json, JsonOptions);
                if (profile != null)
                {
                    Console.WriteLine($"  {profile.Name} - {profile.Displays.Count} display(s), saved {profile.SavedAt:yyyy-MM-dd HH:mm:ss}");
                }
            }
            catch
            {
                Console.WriteLine($"  {Path.GetFileNameWithoutExtension(file)} - (corrupt)");
            }
        }
    }
}
