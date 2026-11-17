using System;
using System.Windows;
using KidsColoringBook.Models;
using KidsColoringBook.Storage;

namespace KidsColoringBook.ViewModels;

public class MainViewModel : ViewModelBase
{
    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    private bool _isFullScreen;
    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => SetProperty(ref _isFullScreen, value);
    }

    public DashboardViewModel Dashboard { get; }
    public GalleryViewModel Gallery { get; }
    public ColoringViewModel Coloring { get; }
    public DrawOwnViewModel DrawOwn { get; }
    public MyDrawingsViewModel MyDrawings { get; }
    public PrintGalleryViewModel PrintGallery { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel(Navigate);
        Gallery = new GalleryViewModel(Navigate);
        Coloring = new ColoringViewModel(Navigate);
        DrawOwn = new DrawOwnViewModel(Navigate);
        MyDrawings = new MyDrawingsViewModel(Navigate);
        PrintGallery = new PrintGalleryViewModel(Navigate);
        Settings = new SettingsViewModel(Navigate);

        Settings.FullScreenChanged += (s, fs) => IsFullScreen = fs;
        var settings = StorageService.Instance.LoadSettings();
        IsFullScreen = settings.IsFullScreen;

        // Start on Dashboard
        CurrentView = Dashboard;
    }

    public void Navigate(string destination, object? parameter)
    {
        switch (destination)
        {
            case "Dashboard":
                CurrentView = Dashboard;
                break;
            case "Gallery":
                Gallery.LoadData();
                CurrentView = Gallery;
                break;
            case "Coloring":
                if (parameter is SketchItem sketch)
                {
                    Coloring.LoadSketch(sketch);
                }
                else if (parameter is UserDrawing drawing)
                {
                    Coloring.LoadUserArtwork(drawing);
                }
                CurrentView = Coloring;
                break;
            case "DrawOwn":
                DrawOwn.ResetBlankCanvas();
                CurrentView = DrawOwn;
                break;
            case "MyDrawings":
                MyDrawings.LoadDrawings();
                CurrentView = MyDrawings;
                break;
            case "PrintGallery":
                PrintGallery.FilterItems();
                CurrentView = PrintGallery;
                break;
            case "Settings":
                Settings.StorageUsageMb = StorageService.Instance.GetStorageUsageMb();
                CurrentView = Settings;
                break;
        }
    }
}
