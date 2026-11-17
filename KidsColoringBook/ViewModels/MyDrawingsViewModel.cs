using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using KidsColoringBook.DrawingEngine;
using KidsColoringBook.Models;
using KidsColoringBook.Printing;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;
using KidsColoringBook.Views;
using Microsoft.Win32;

namespace KidsColoringBook.ViewModels;

public class MyDrawingsViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;

    public ObservableCollection<UserDrawing> SavedDrawings { get; } = new();

    private UserDrawing? _selectedDrawing;
    public UserDrawing? SelectedDrawing
    {
        get => _selectedDrawing;
        set => SetProperty(ref _selectedDrawing, value);
    }

    public bool HasDrawings => SavedDrawings.Count > 0;

    public ICommand OpenDrawingCommand { get; }
    public ICommand RenameDrawingCommand { get; }
    public ICommand DeleteDrawingCommand { get; }
    public ICommand PrintDrawingCommand { get; }
    public ICommand ExportPngCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand BackCommand { get; }

    public MyDrawingsViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        OpenDrawingCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null) return;
            SoundService.Instance.PlayClick();

            if (item.IsCustomSketch)
            {
                // Continue in DrawOwn or Coloring
                _navigate("Coloring", item);
            }
            else
            {
                _navigate("Coloring", item);
            }
        });

        RenameDrawingCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null) return;
            SoundService.Instance.PlayClick();

            var dlg = new TextInputDialogWindow("Rename Artwork", item.Title);
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.InputText))
            {
                StorageService.Instance.RenameUserDrawing(item.Id, dlg.InputText);
                LoadDrawings();
            }
        });

        DeleteDrawingCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null) return;
            SoundService.Instance.PlayClick();

            var confirm = new ConfirmDialogWindow("Delete Drawing", $"Are you sure you want to delete '{item.Title}'? This cannot be undone.");
            if (confirm.ShowDialog() == true)
            {
                StorageService.Instance.DeleteUserDrawing(item.Id);
                LoadDrawings();
            }
        });

        PrintDrawingCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null || !File.Exists(item.ImagePath)) return;
            SoundService.Instance.PlayClick();

            var bmp = CanvasExporter.LoadFromPng(item.ImagePath);
            var settings = StorageService.Instance.LoadSettings();
            var preview = new PrintPreviewWindow(bmp, item.Title, item.ChildName ?? settings.ChildName, false);
            preview.ShowDialog();
        });

        ExportPngCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null || !File.Exists(item.ImagePath)) return;
            SoundService.Instance.PlayClick();

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = $"{item.Title.Replace(" ", "_")}.png",
                DefaultExt = ".png"
            };

            if (dlg.ShowDialog() == true)
            {
                File.Copy(item.ImagePath, dlg.FileName, true);
                MessageBox.Show("Drawing saved as PNG!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });

        ExportPdfCommand = new RelayCommand(p =>
        {
            var item = p as UserDrawing ?? SelectedDrawing;
            if (item == null || !File.Exists(item.ImagePath)) return;
            SoundService.Instance.PlayClick();

            var bmp = CanvasExporter.LoadFromPng(item.ImagePath);
            var settings = StorageService.Instance.LoadSettings();
            var page = PrintService.CreatePrintPage(bmp, item.Title, item.ChildName ?? settings.ChildName, item.ModifiedDate);

            var dlg = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf|XPS Document (*.xps)|*.xps",
                FileName = $"{item.Title.Replace(" ", "_")}.pdf",
                DefaultExt = ".pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                PrintService.ExportToPdfOrXps(page, dlg.FileName);
                MessageBox.Show("Drawing saved as PDF!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Dashboard", null);
        });

        LoadDrawings();
    }

    public void LoadDrawings()
    {
        SavedDrawings.Clear();
        var list = StorageService.Instance.LoadUserDrawings();
        foreach (var d in list)
        {
            SavedDrawings.Add(d);
        }
        OnPropertyChanged(nameof(HasDrawings));
    }
}
