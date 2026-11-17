using System;
using System.Windows;
using System.Windows.Input;
using KidsColoringBook.Services;

namespace KidsColoringBook.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;

    public ICommand StartColoringCommand { get; }
    public ICommand MyDrawingsCommand { get; }
    public ICommand DrawOwnCommand { get; }
    public ICommand PrintGalleryCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    public DashboardViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        StartColoringCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Gallery", null);
        });

        MyDrawingsCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("MyDrawings", null);
        });

        DrawOwnCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("DrawOwn", null);
        });

        PrintGalleryCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("PrintGallery", null);
        });

        SettingsCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Settings", null);
        });

        ExitCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            Application.Current.Shutdown();
        });
    }
}
