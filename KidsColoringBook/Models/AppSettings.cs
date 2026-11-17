namespace KidsColoringBook.Models;

public class AppSettings
{
    public string ChildName { get; set; } = "Rohit";
    public bool IsFullScreen { get; set; } = false;
    public bool SoundEffectsEnabled { get; set; } = true;
    public double DefaultBrushThickness { get; set; } = 8.0;
    public List<string> RecentColors { get; set; } = new()
    {
        "#FFFF0000", "#FF00FF00", "#FF0000FF", "#FFFFFF00", "#FFFFA500"
    };
}
