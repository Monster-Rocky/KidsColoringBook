using System.Collections.Generic;
using System.Windows.Media;
using KidsColoringBook.Models;

namespace KidsColoringBook.Configuration;

public static class ColorPalette
{
    public static List<PaletteColor> DefaultPalette => new()
    {
        new PaletteColor("Red", Color.FromRgb(244, 67, 54)),
        new PaletteColor("Blue", Color.FromRgb(33, 150, 243)),
        new PaletteColor("Green", Color.FromRgb(76, 175, 80)),
        new PaletteColor("Yellow", Color.FromRgb(255, 235, 59)),
        new PaletteColor("Orange", Color.FromRgb(255, 152, 0)),
        new PaletteColor("Pink", Color.FromRgb(233, 30, 99)),
        new PaletteColor("Purple", Color.FromRgb(156, 39, 176)),
        new PaletteColor("Brown", Color.FromRgb(121, 85, 72)),
        new PaletteColor("Black", Color.FromRgb(0, 0, 0)),
        new PaletteColor("White", Color.FromRgb(255, 255, 255)),
        new PaletteColor("Grey", Color.FromRgb(158, 158, 158)),
        new PaletteColor("Light Blue", Color.FromRgb(129, 212, 250)),
        new PaletteColor("Dark Blue", Color.FromRgb(13, 71, 161)),
        new PaletteColor("Light Green", Color.FromRgb(165, 214, 167)),
        new PaletteColor("Dark Green", Color.FromRgb(27, 94, 32)),
        new PaletteColor("Skin Tone", Color.FromRgb(255, 204, 153)),
        new PaletteColor("Coral", Color.FromRgb(255, 112, 67)),
        new PaletteColor("Lime", Color.FromRgb(205, 220, 57)),
        new PaletteColor("Teal", Color.FromRgb(0, 150, 136)),
        new PaletteColor("Indigo", Color.FromRgb(63, 81, 181)),
        new PaletteColor("Deep Purple", Color.FromRgb(103, 58, 183)),
        new PaletteColor("Amber Gold", Color.FromRgb(255, 193, 7)),
        new PaletteColor("Peach", Color.FromRgb(255, 171, 145)),
        new PaletteColor("Lavender", Color.FromRgb(209, 196, 233))
    };
}
