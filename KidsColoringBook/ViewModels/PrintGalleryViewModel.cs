using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using KidsColoringBook.DrawingEngine;
using KidsColoringBook.Models;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;
using KidsColoringBook.Views;

namespace KidsColoringBook.ViewModels;

public class PrintGalleryViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;

    private string _activeTab = "OriginalSketches"; // OriginalSketches, ColouredArtwork
    public string ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                OnPropertyChanged(nameof(IsOriginalTab));
                OnPropertyChanged(nameof(IsColouredTab));
                FilterItems();
            }
        }
    }

    public bool IsOriginalTab => ActiveTab == "OriginalSketches";
    public bool IsColouredTab => ActiveTab == "ColouredArtwork";

    private string _selectedDifficulty = "Easy";
    public string SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (SetProperty(ref _selectedDifficulty, value))
            {
                FilterItems();
            }
        }
    }

    public ObservableCollection<SketchItem> OriginalSketches { get; } = new();
    public ObservableCollection<UserDrawing> ColouredDrawings { get; } = new();

    public ICommand SelectTabCommand { get; }
    public ICommand SelectDifficultyCommand { get; }
    public ICommand PrintSketchOriginalCommand { get; }
    public ICommand PrintUserArtworkCommand { get; }
    public ICommand BackCommand { get; }

    public PrintGalleryViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        SelectTabCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is string tab) ActiveTab = tab;
        });

        SelectDifficultyCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is string diff) SelectedDifficulty = diff;
        });

        PrintSketchOriginalCommand = new RelayCommand(p =>
        {
            if (p is SketchItem sketch && File.Exists(sketch.PngPath))
            {
                SoundService.Instance.PlayClick();
                var bmp = CanvasExporter.LoadFromPng(sketch.PngPath);
                var settings = StorageService.Instance.LoadSettings();
                var preview = new PrintPreviewWindow(bmp, sketch.Title + " (Outline)", settings.ChildName, true);
                preview.ShowDialog();
            }
        });

        PrintUserArtworkCommand = new RelayCommand(p =>
        {
            if (p is UserDrawing drawing && File.Exists(drawing.ImagePath))
            {
                SoundService.Instance.PlayClick();
                var bmp = CanvasExporter.LoadFromPng(drawing.ImagePath);
                var settings = StorageService.Instance.LoadSettings();
                var preview = new PrintPreviewWindow(bmp, drawing.Title, drawing.ChildName ?? settings.ChildName, false);
                preview.ShowDialog();
            }
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Dashboard", null);
        });

        FilterItems();
    }

    public void FilterItems()
    {
        OriginalSketches.Clear();
        ColouredDrawings.Clear();

        if (IsOriginalTab)
        {
            var sketches = StorageService.Instance.LoadAllSketches()
                .Where(s => string.Equals(s.Difficulty, SelectedDifficulty, StringComparison.OrdinalIgnoreCase));
            foreach (var s in sketches)
            {
                OriginalSketches.Add(s);
            }
        }
        else
        {
            var drawings = StorageService.Instance.LoadUserDrawings();
            foreach (var d in drawings)
            {
                ColouredDrawings.Add(d);
            }
        }
    }
}
