using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KidsColoringBook.Configuration;
using KidsColoringBook.DrawingEngine;
using KidsColoringBook.Models;
using KidsColoringBook.Printing;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;
using KidsColoringBook.Views;

namespace KidsColoringBook.ViewModels;

public class ColoringViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;
    private readonly HistoryManager _history = new(20);
    private readonly StrokeSmoother _smoother = new();

    private SketchItem _currentSketch = new();
    public SketchItem CurrentSketch
    {
        get => _currentSketch;
        set => SetProperty(ref _currentSketch, value);
    }

    private WriteableBitmap? _canvasBitmap;
    public WriteableBitmap? CanvasBitmap
    {
        get => _canvasBitmap;
        set => SetProperty(ref _canvasBitmap, value);
    }

    private WriteableBitmap? _originalBitmap;
    public WriteableBitmap? OriginalBitmap
    {
        get => _originalBitmap;
        set => SetProperty(ref _originalBitmap, value);
    }

    private DrawingTool _currentTool = DrawingTool.Fill; // Default tool is Fill for coloring book!
    public DrawingTool CurrentTool
    {
        get => _currentTool;
        set
        {
            if (SetProperty(ref _currentTool, value))
            {
                OnPropertyChanged(nameof(IsPencilSelected));
                OnPropertyChanged(nameof(IsFillSelected));
                OnPropertyChanged(nameof(IsEraserSelected));
            }
        }
    }

    public bool IsPencilSelected => CurrentTool == DrawingTool.Pencil;
    public bool IsFillSelected => CurrentTool == DrawingTool.Fill;
    public bool IsEraserSelected => CurrentTool == DrawingTool.Eraser;

    private Color _selectedColor = Color.FromRgb(244, 67, 54); // Red default
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (SetProperty(ref _selectedColor, value))
            {
                OnPropertyChanged(nameof(SelectedColorBrush));
                OnPropertyChanged(nameof(BrushPreviewColorBrush));
                AddRecentColor(value);
            }
        }
    }

    public SolidColorBrush SelectedColorBrush => new(SelectedColor);
    public SolidColorBrush BrushPreviewColorBrush => IsEraserSelected ? new SolidColorBrush(Color.FromRgb(203, 213, 225)) : SelectedColorBrush;

    private double _brushThickness = 8.0;
    public double BrushThickness
    {
        get => _brushThickness;
        set
        {
            if (SetProperty(ref _brushThickness, value))
            {
                OnPropertyChanged(nameof(BrushThicknessDescription));
                OnPropertyChanged(nameof(BrushPreviewSize));
                OnPropertyChanged(nameof(IsThinBrushSelected));
                OnPropertyChanged(nameof(IsMediumBrushSelected));
                OnPropertyChanged(nameof(IsThickBrushSelected));
                OnPropertyChanged(nameof(IsJumboBrushSelected));
            }
        }
    }

    public double BrushPreviewSize => Math.Clamp(BrushThickness, 4.0, 44.0);

    public string BrushThicknessDescription
    {
        get
        {
            if (Math.Abs(BrushThickness - 4.0) < 0.5) return "Thin • 4 px";
            if (Math.Abs(BrushThickness - 8.0) < 0.5) return "Medium • 8 px";
            if (Math.Abs(BrushThickness - 16.0) < 0.5) return "Thick • 16 px";
            if (Math.Abs(BrushThickness - 28.0) < 0.5) return "Jumbo • 28 px";
            return $"Custom • {Math.Round(BrushThickness)} px";
        }
    }

    public bool IsThinBrushSelected => Math.Abs(BrushThickness - 4.0) < 0.5;
    public bool IsMediumBrushSelected => Math.Abs(BrushThickness - 8.0) < 0.5;
    public bool IsThickBrushSelected => Math.Abs(BrushThickness - 16.0) < 0.5;
    public bool IsJumboBrushSelected => Math.Abs(BrushThickness - 28.0) < 0.5;

    private double _zoomLevel = 1.0;
    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.5, 4.0));
    }

    public ObservableCollection<PaletteColor> PaletteColors { get; } = new();
    public ObservableCollection<PaletteColor> RecentColors { get; } = new();

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    // Commands
    public ICommand SelectToolCommand { get; }
    public ICommand SelectColorCommand { get; }
    public ICommand SelectThicknessCommand { get; }
    public ICommand PickCustomColorCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand ClearAllCommand { get; }
    public ICommand ResetOriginalCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand FitToScreenCommand { get; }
    public ICommand SaveArtworkCommand { get; }
    public ICommand PrintColouredCommand { get; }
    public ICommand PrintOriginalCommand { get; }
    public ICommand BackCommand { get; }

    public ColoringViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        // Initialize predefined palette
        foreach (var c in ColorPalette.DefaultPalette)
        {
            PaletteColors.Add(c);
        }

        // Initialize recent colors from settings
        var settings = StorageService.Instance.LoadSettings();
        _brushThickness = settings.DefaultBrushThickness;
        foreach (var hex in settings.RecentColors.Take(8))
        {
            try
            {
                RecentColors.Add(new PaletteColor("Recent", hex));
            }
            catch { }
        }

        _history.HistoryChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        };

        SelectToolCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is DrawingTool tool) CurrentTool = tool;
            else if (p is string toolStr && Enum.TryParse<DrawingTool>(toolStr, true, out var t)) CurrentTool = t;
        });

        SelectColorCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is PaletteColor pc) SelectedColor = pc.Color;
            else if (p is Color c) SelectedColor = c;
        });

        SelectThicknessCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p != null && double.TryParse(p.ToString(), out double th))
            {
                BrushThickness = th;
            }
        });

        PickCustomColorCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            var dlg = new ColorPickerDialogWindow(SelectedColor);
            if (dlg.ShowDialog() == true)
            {
                SelectedColor = dlg.SelectedColor;
            }
        });

        UndoCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap != null && _history.CanUndo)
            {
                SoundService.Instance.PlayClick();
                _history.Undo(CanvasBitmap);
            }
        }, () => CanUndo);

        RedoCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap != null && _history.CanRedo)
            {
                SoundService.Instance.PlayClick();
                _history.Redo(CanvasBitmap);
            }
        }, () => CanRedo);

        ClearAllCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            var confirm = new ConfirmDialogWindow("Clear Coloring", "Do you want to clear your coloring and start over?");
            if (confirm.ShowDialog() == true)
            {
                SoundService.Instance.PlayPop();
                _history.PushState(CanvasBitmap);
                if (OriginalBitmap != null)
                {
                    // Restore original sketch outline
                    var restored = CanvasExporter.CloneWriteableBitmap(OriginalBitmap);
                    ApplyBitmap(restored);
                }
            }
        });

        ResetOriginalCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null || OriginalBitmap == null) return;
            var confirm = new ConfirmDialogWindow("Reset Original Sketch", "Are you sure you want to delete the current coloring and reset the original sketch?");
            if (confirm.ShowDialog() == true)
            {
                SoundService.Instance.PlayPop();
                _history.PushState(CanvasBitmap);
                var restored = CanvasExporter.CloneWriteableBitmap(OriginalBitmap);
                ApplyBitmap(restored);
            }
        });

        ZoomInCommand = new RelayCommand(() => ZoomLevel += 0.25);
        ZoomOutCommand = new RelayCommand(() => ZoomLevel -= 0.25);
        FitToScreenCommand = new RelayCommand(() => ZoomLevel = 1.0);

        SaveArtworkCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            SoundService.Instance.PlaySuccess();

            var currentSettings = StorageService.Instance.LoadSettings();
            var drawing = new UserDrawing
            {
                Title = CurrentSketch.Title,
                OriginalSketchId = CurrentSketch.Id,
                Difficulty = CurrentSketch.Difficulty,
                IsCustomSketch = false,
                ChildName = currentSettings.ChildName
            };

            StorageService.Instance.SaveUserDrawing(drawing, CanvasBitmap);
            MessageBox.Show("Great job! Your artwork has been saved to 'My Drawings'!", "Artwork Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        });

        PrintColouredCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            SoundService.Instance.PlayClick();
            var settings = StorageService.Instance.LoadSettings();
            var preview = new PrintPreviewWindow(CanvasBitmap, CurrentSketch.Title, settings.ChildName, false);
            preview.ShowDialog();
        });

        PrintOriginalCommand = new RelayCommand(() =>
        {
            if (OriginalBitmap == null) return;
            SoundService.Instance.PlayClick();
            var settings = StorageService.Instance.LoadSettings();
            var preview = new PrintPreviewWindow(OriginalBitmap, CurrentSketch.Title + " (Outline)", settings.ChildName, true);
            preview.ShowDialog();
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Gallery", null);
        });
    }

    public void LoadSketch(SketchItem sketch)
    {
        CurrentSketch = sketch;
        ZoomLevel = 1.0;

        WriteableBitmap? wb = null;
        if (File.Exists(sketch.PngPath))
        {
            wb = CanvasExporter.LoadFromPng(sketch.PngPath);
        }
        else
        {
            // Fallback: create 800x800 blank canvas
            wb = CanvasExporter.CreateBlankCanvas(800, 800);
        }

        OriginalBitmap = CanvasExporter.CloneWriteableBitmap(wb);
        CanvasBitmap = wb;
        _history.Initialize(CanvasBitmap);
    }

    public void LoadUserArtwork(UserDrawing drawing)
    {
        CurrentSketch = new SketchItem
        {
            Id = drawing.OriginalSketchId ?? drawing.Id,
            Title = drawing.Title,
            Difficulty = drawing.Difficulty ?? "Custom"
        };
        ZoomLevel = 1.0;

        if (File.Exists(drawing.ImagePath))
        {
            CanvasBitmap = CanvasExporter.LoadFromPng(drawing.ImagePath);
        }
        else
        {
            CanvasBitmap = CanvasExporter.CreateBlankCanvas(800, 800);
        }

        // Try loading original sketch if available
        if (!string.IsNullOrEmpty(drawing.OriginalSketchId))
        {
            var all = StorageService.Instance.LoadAllSketches();
            var orig = all.FirstOrDefault(s => s.Id == drawing.OriginalSketchId);
            if (orig != null && File.Exists(orig.PngPath))
            {
                OriginalBitmap = CanvasExporter.LoadFromPng(orig.PngPath);
            }
            else
            {
                OriginalBitmap = CanvasExporter.CloneWriteableBitmap(CanvasBitmap);
            }
        }
        else
        {
            OriginalBitmap = CanvasExporter.CloneWriteableBitmap(CanvasBitmap);
        }

        _history.Initialize(CanvasBitmap);
    }

    public void OnPointerDown(Point point)
    {
        if (CanvasBitmap == null) return;

        if (CurrentTool == DrawingTool.Fill)
        {
            _history.PushState(CanvasBitmap);
            bool filled = FloodFillEngine.Fill(CanvasBitmap, point, SelectedColor);
            if (filled)
            {
                SoundService.Instance.PlayPop();
            }
        }
        else if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser)
        {
            _history.PushState(CanvasBitmap);
            Color strokeColor = (CurrentTool == DrawingTool.Eraser) ? Colors.White : SelectedColor;
            _smoother.Start(CanvasBitmap, point, strokeColor, BrushThickness);
        }
    }

    public void OnPointerMove(Point currentPoint)
    {
        if (CanvasBitmap == null) return;

        if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser)
        {
            Color strokeColor = (CurrentTool == DrawingTool.Eraser) ? Colors.White : SelectedColor;
            _smoother.Move(CanvasBitmap, currentPoint, strokeColor, BrushThickness);
        }
    }

    public void OnPointerMove(Point prevPoint, Point currentPoint)
    {
        OnPointerMove(currentPoint);
    }

    public void OnPointerUp(Point? point = null)
    {
        if (CanvasBitmap == null) return;

        if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser)
        {
            Color strokeColor = (CurrentTool == DrawingTool.Eraser) ? Colors.White : SelectedColor;
            _smoother.End(CanvasBitmap, point, strokeColor, BrushThickness);
        }
    }

    private void ApplyBitmap(WriteableBitmap newBitmap)
    {
        if (CanvasBitmap == null) return;
        int totalBytes = newBitmap.BackBufferStride * newBitmap.PixelHeight;

        CanvasBitmap.Lock();
        newBitmap.Lock();
        try
        {
            unsafe
            {
                byte* src = (byte*)newBitmap.BackBuffer;
                byte* dst = (byte*)CanvasBitmap.BackBuffer;
                Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
            }
            CanvasBitmap.AddDirtyRect(new Int32Rect(0, 0, CanvasBitmap.PixelWidth, CanvasBitmap.PixelHeight));
        }
        finally
        {
            CanvasBitmap.Unlock();
            newBitmap.Unlock();
        }
    }

    private void AddRecentColor(Color color)
    {
        string hex = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        var existing = RecentColors.FirstOrDefault(c => string.Equals(c.Hex, hex, StringComparison.OrdinalIgnoreCase));
        if (existing != null) RecentColors.Remove(existing);

        RecentColors.Insert(0, new PaletteColor("Recent", color));
        while (RecentColors.Count > 8) RecentColors.RemoveAt(RecentColors.Count - 1);

        // Update settings
        var settings = StorageService.Instance.LoadSettings();
        settings.RecentColors = RecentColors.Select(c => c.Hex).ToList();
        StorageService.Instance.SaveSettings(settings);
    }
}
