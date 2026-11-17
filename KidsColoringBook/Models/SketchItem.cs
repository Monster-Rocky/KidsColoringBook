namespace KidsColoringBook.Models;

public class SketchItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy"; // Easy, Medium, Hard
    public string SvgPath { get; set; } = string.Empty;
    public string PngPath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();

    public string DisplaySubtitle => $"{Difficulty} • {Category}";
}
