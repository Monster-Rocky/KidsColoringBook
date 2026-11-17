using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KidsColoringBook.Views;

public partial class ColorPickerDialogWindow : Window
{
    public Color SelectedColor { get; private set; }

    public ColorPickerDialogWindow(Color initialColor)
    {
        InitializeComponent();
        SelectedColor = initialColor;

        SliderR.Value = initialColor.R;
        SliderG.Value = initialColor.G;
        SliderB.Value = initialColor.B;

        UpdatePreview();
    }

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ColorPreview == null) return;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        byte r = (byte)Math.Round(SliderR.Value);
        byte g = (byte)Math.Round(SliderG.Value);
        byte b = (byte)Math.Round(SliderB.Value);
        SelectedColor = Color.FromArgb(255, r, g, b);
        ColorPreview.Fill = new SolidColorBrush(SelectedColor);
    }

    private void OnPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            SliderR.Value = c.R;
            SliderG.Value = c.G;
            SliderB.Value = c.B;
            UpdatePreview();
        }
    }

    private void OnSelectClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
