using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KidsColoringBook.Printing;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;
using Microsoft.Win32;

namespace KidsColoringBook.Views;

public partial class PrintPreviewWindow : Window
{
    private readonly BitmapSource _artworkBitmap;
    private readonly string _artworkTitle;
    private readonly bool _isOriginalSketch;
    private FrameworkElement? _currentPageElement;

    public PrintPreviewWindow(BitmapSource artworkBitmap, string title, string defaultChildName, bool isOriginalSketch)
    {
        InitializeComponent();
        _artworkBitmap = artworkBitmap;
        _artworkTitle = title;
        _isOriginalSketch = isOriginalSketch;

        TxtChildName.Text = string.IsNullOrWhiteSpace(defaultChildName) ? "Rohit" : defaultChildName;
        DatePickerControl.SelectedDate = DateTime.Today;

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        string name = TxtChildName.Text.Trim();
        DateTime date = DatePickerControl.SelectedDate ?? DateTime.Today;
        bool isLandscape = RbLandscape.IsChecked == true;

        _currentPageElement = PrintService.CreatePrintPage(_artworkBitmap, _artworkTitle, name, date, isLandscape);
        PreviewHost.Content = _currentPageElement;
    }

    private void OnNameOrDateChanged(object sender, EventArgs e)
    {
        if (IsLoaded)
        {
            RefreshPreview();
        }
    }

    private void OnOrientationChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            RefreshPreview();
        }
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (_currentPageElement == null) return;
        SoundService.Instance.PlayClick();

        // Save current name to settings for future convenience
        var settings = StorageService.Instance.LoadSettings();
        settings.ChildName = TxtChildName.Text.Trim();
        StorageService.Instance.SaveSettings(settings);

        bool isLandscape = RbLandscape.IsChecked == true;
        bool success = PrintService.Print(_currentPageElement, _artworkTitle, isLandscape);
        if (success)
        {
            MessageBox.Show("Printing completed successfully!", "Printed", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnSavePdfClick(object sender, RoutedEventArgs e)
    {
        if (_currentPageElement == null) return;
        SoundService.Instance.PlayClick();

        var dlg = new SaveFileDialog
        {
            Filter = "PDF Document (*.pdf)|*.pdf|XPS Document (*.xps)|*.xps",
            FileName = $"{_artworkTitle.Replace(" ", "_")}_Print.pdf",
            DefaultExt = ".pdf"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                bool isLandscape = RbLandscape.IsChecked == true;
                PrintService.ExportToPdfOrXps(_currentPageElement, dlg.FileName, isLandscape);
                MessageBox.Show($"File successfully saved to:\n{dlg.FileName}", "Saved Successfully", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save file: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnSavePngClick(object sender, RoutedEventArgs e)
    {
        if (_currentPageElement == null) return;
        SoundService.Instance.PlayClick();

        var dlg = new SaveFileDialog
        {
            Filter = "PNG Image (*.png)|*.png",
            FileName = $"{_artworkTitle.Replace(" ", "_")}_Framed.png",
            DefaultExt = ".png"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                bool isLandscape = RbLandscape.IsChecked == true;
                double w = isLandscape ? PrintService.A4Height : PrintService.A4Width;
                double h = isLandscape ? PrintService.A4Width : PrintService.A4Height;
                var rtb = PrintService.RenderPageToBitmap(_currentPageElement, w, h);
                KidsColoringBook.DrawingEngine.CanvasExporter.SaveToPng(rtb, dlg.FileName);
                MessageBox.Show($"Framed drawing successfully saved to:\n{dlg.FileName}", "Saved Successfully", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save image: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        SoundService.Instance.PlayClick();
        Close();
    }
}
