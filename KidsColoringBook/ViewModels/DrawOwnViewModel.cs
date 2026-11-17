using System;
using System.Collections.ObjectModel;
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
using Microsoft.Win32;

namespace KidsColoringBook.ViewModels;

public class DrawOwnViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;
    private readonly HistoryManager _history = new(25);
    private readonly StrokeSmoother _smoother = new();

    private WriteableBitmap? _canvasBitmap;
    public WriteableBitmap? CanvasBitmap
    {
        get => _canvasBitmap;
        set => SetProperty(ref _canvasBitmap, value);
    }

    private DrawingTool _currentTool = DrawingTool.Pencil;
    public DrawingTool CurrentTool
    {
        get => _currentTool;
        set
        {
            if (SetProperty(ref _currentTool, value))
            {
                OnPropertyChanged(nameof(IsPencilSelected));
                OnPropertyChanged(nameof(IsEraserSelected));
                OnPropertyChanged(nameof(IsLineSelected));
                OnPropertyChanged(nameof(IsRectangleSelected));
                OnPropertyChanged(nameof(IsCircleSelected));
                OnPropertyChanged(nameof(IsStarSelected));
                OnPropertyChanged(nameof(IsHeartSelected));
            }
        }
    }

    public bool IsPencilSelected => CurrentTool == DrawingTool.Pencil;
    public bool IsEraserSelected => CurrentTool == DrawingTool.Eraser;
    public bool IsLineSelected => CurrentTool == DrawingTool.Line;
    public bool IsRectangleSelected => CurrentTool == DrawingTool.Rectangle;
    public bool IsCircleSelected => CurrentTool == DrawingTool.Circle;
    public bool IsStarSelected => CurrentTool == DrawingTool.Star;
    public bool IsHeartSelected => CurrentTool == DrawingTool.Heart;

    private Color _selectedColor = Color.FromRgb(33, 150, 243); // Blue default
    public Color SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (SetProperty(ref _selectedColor, value))
            {
                OnPropertyChanged(nameof(SelectedColorBrush));
                OnPropertyChanged(nameof(BrushPreviewColorBrush));
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

    public ObservableCollection<PaletteColor> PaletteColors { get; } = new();

    public bool CanUndo => _history.CanUndo;
    public bool CanRedo => _history.CanRedo;

    private Point _shapeStartPoint;
    private WriteableBitmap? _shapePreviewSnapshot;

    // Commands
    public ICommand SelectToolCommand { get; }
    public ICommand SelectColorCommand { get; }
    public ICommand SelectThicknessCommand { get; }
    public ICommand PickCustomColorCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand ClearCanvasCommand { get; }
    public ICommand SaveDrawingCommand { get; }
    public ICommand ExportPngCommand { get; }
    public ICommand PrintDrawingCommand { get; }
    public ICommand BackCommand { get; }

    public DrawOwnViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        foreach (var c in ColorPalette.DefaultPalette)
        {
            PaletteColors.Add(c);
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
            else if (p is string s && Enum.TryParse<DrawingTool>(s, true, out var t)) CurrentTool = t;
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

        ClearCanvasCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            var confirm = new ConfirmDialogWindow("Clear Canvas", "Do you want to clear the entire canvas and start a new sketch?");
            if (confirm.ShowDialog() == true)
            {
                SoundService.Instance.PlayPop();
                _history.PushState(CanvasBitmap);
                StrokeEngine.FillRect(CanvasBitmap, 0, 0, CanvasBitmap.PixelWidth, CanvasBitmap.PixelHeight, Colors.White);
            }
        });

        SaveDrawingCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            SoundService.Instance.PlaySuccess();

            var dlg = new TextInputDialogWindow("Name Your Drawing", "My Beautiful Sketch");
            if (dlg.ShowDialog() == true)
            {
                string title = string.IsNullOrWhiteSpace(dlg.InputText) ? "My Beautiful Sketch" : dlg.InputText;
                var settings = StorageService.Instance.LoadSettings();

                var drawing = new UserDrawing
                {
                    Title = title,
                    OriginalSketchId = null,
                    IsCustomSketch = true,
                    ChildName = settings.ChildName
                };

                StorageService.Instance.SaveUserDrawing(drawing, CanvasBitmap);
                MessageBox.Show("Saved! You can view or print your artwork anytime in 'My Drawings'!", "Artwork Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });

        ExportPngCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            SoundService.Instance.PlayClick();

            var dlg = new SaveFileDialog
            {
                Filter = "PNG Image (*.png)|*.png",
                FileName = "MySketch.png",
                DefaultExt = ".png"
            };

            if (dlg.ShowDialog() == true)
            {
                CanvasExporter.SaveToPng(CanvasBitmap, dlg.FileName);
                MessageBox.Show("Drawing saved as PNG image!", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });

        PrintDrawingCommand = new RelayCommand(() =>
        {
            if (CanvasBitmap == null) return;
            SoundService.Instance.PlayClick();
            var settings = StorageService.Instance.LoadSettings();
            var preview = new PrintPreviewWindow(CanvasBitmap, "My Custom Sketch", settings.ChildName, false);
            preview.ShowDialog();
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Dashboard", null);
        });

        ResetBlankCanvas();
    }

    public void ResetBlankCanvas()
    {
        CanvasBitmap = CanvasExporter.CreateBlankCanvas(800, 800);
        _history.Initialize(CanvasBitmap);
    }

    public void OnPointerDown(Point point)
    {
        if (CanvasBitmap == null) return;

        _history.PushState(CanvasBitmap);

        if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser)
        {
            Color strokeColor = (CurrentTool == DrawingTool.Eraser) ? Colors.White : SelectedColor;
            _smoother.Start(CanvasBitmap, point, strokeColor, BrushThickness);
        }
        else
        {
            // For geometric shapes: record start point and snapshot for interactive rubber-banding
            _shapeStartPoint = point;
            _shapePreviewSnapshot = CanvasExporter.CloneWriteableBitmap(CanvasBitmap);
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
        else if (_shapePreviewSnapshot != null)
        {
            // Restore snapshot and draw current preview shape
            RestoreFromSnapshot(_shapePreviewSnapshot);
            DrawCurrentShape(_shapeStartPoint, currentPoint);
        }
    }

    public void OnPointerMove(Point prevPoint, Point currentPoint)
    {
        OnPointerMove(currentPoint);
    }

    public void OnPointerUp(Point currentPoint)
    {
        if (CanvasBitmap == null) return;

        if (CurrentTool == DrawingTool.Pencil || CurrentTool == DrawingTool.Eraser)
        {
            Color strokeColor = (CurrentTool == DrawingTool.Eraser) ? Colors.White : SelectedColor;
            _smoother.End(CanvasBitmap, currentPoint, strokeColor, BrushThickness);
        }
        else if (_shapePreviewSnapshot != null)
        {
            RestoreFromSnapshot(_shapePreviewSnapshot);
            DrawCurrentShape(_shapeStartPoint, currentPoint);
            _shapePreviewSnapshot = null;
        }
    }

    private void DrawCurrentShape(Point start, Point end)
    {
        if (CanvasBitmap == null) return;

        switch (CurrentTool)
        {
            case DrawingTool.Line:
                StrokeEngine.DrawLineSegment(CanvasBitmap, start, end, SelectedColor, BrushThickness);
                break;
            case DrawingTool.Rectangle:
                StrokeEngine.DrawRectangle(CanvasBitmap, start, end, SelectedColor, BrushThickness);
                break;
            case DrawingTool.Circle:
                StrokeEngine.DrawEllipse(CanvasBitmap, start, end, SelectedColor, BrushThickness);
                break;
            case DrawingTool.Star:
                StrokeEngine.DrawStar(CanvasBitmap, start, end, SelectedColor, BrushThickness);
                break;
            case DrawingTool.Heart:
                StrokeEngine.DrawHeart(CanvasBitmap, start, end, SelectedColor, BrushThickness);
                break;
        }
    }

    private void RestoreFromSnapshot(WriteableBitmap snapshot)
    {
        if (CanvasBitmap == null) return;
        int totalBytes = snapshot.BackBufferStride * snapshot.PixelHeight;

        CanvasBitmap.Lock();
        snapshot.Lock();
        try
        {
            unsafe
            {
                byte* src = (byte*)snapshot.BackBuffer;
                byte* dst = (byte*)CanvasBitmap.BackBuffer;
                Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
            }
            CanvasBitmap.AddDirtyRect(new Int32Rect(0, 0, CanvasBitmap.PixelWidth, CanvasBitmap.PixelHeight));
        }
        finally
        {
            CanvasBitmap.Unlock();
            snapshot.Unlock();
        }
    }
}
