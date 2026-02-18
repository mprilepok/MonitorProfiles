namespace MonitorProfiles.Models;

public class DisplayProfile
{
    public string Name { get; set; } = "";
    public DateTime SavedAt { get; set; }
    public List<DisplayInfo> Displays { get; set; } = [];
}
