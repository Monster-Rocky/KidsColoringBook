using System.Windows.Media;

namespace KidsColoringBook.Models;

public class PaletteColor
{
    public string Name { get; set; } = string.Empty;
    public Color Color { get; set; }
    public string Hex { get; set; } = string.Empty;

    public SolidColorBrush Brush => new(Color);

    public PaletteColor() { }

    public PaletteColor(string name, Color color)
    {
        Name = name;
        Color = color;
        Hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    public PaletteColor(string name, string hex)
    {
        Name = name;
        Hex = hex;
        Color = (Color)ColorConverter.ConvertFromString(hex);
    }
}
