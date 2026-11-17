using System;
using System.Windows;
using System.Windows.Input;
using KidsColoringBook.Models;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;
using KidsColoringBook.Views;

namespace KidsColoringBook.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;
    private AppSettings _settings;

    public string ChildName
    {
        get => _settings.ChildName;
        set
        {
            if (_settings.ChildName != value)
            {
                _settings.ChildName = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }
    }

    public bool IsFullScreen
    {
        get => _settings.IsFullScreen;
        set
        {
            if (_settings.IsFullScreen != value)
            {
                _settings.IsFullScreen = value;
                OnPropertyChanged();
                SaveSettings();
                FullScreenChanged?.Invoke(this, value);
            }
        }
    }

    public bool SoundEffectsEnabled
    {
        get => _settings.SoundEffectsEnabled;
        set
        {
            if (_settings.SoundEffectsEnabled != value)
            {
                _settings.SoundEffectsEnabled = value;
                OnPropertyChanged();
                SaveSettings();
                SoundService.Instance.IsSoundEnabled = value;
            }
        }
    }

    public double DefaultBrushThickness
    {
        get => _settings.DefaultBrushThickness;
        set
        {
            if (Math.Abs(_settings.DefaultBrushThickness - value) > 0.1)
            {
                _settings.DefaultBrushThickness = value;
                OnPropertyChanged();
                SaveSettings();
            }
        }
    }

    private double _storageUsageMb;
    public double StorageUsageMb
    {
        get => _storageUsageMb;
        set => SetProperty(ref _storageUsageMb, value);
    }

    public event EventHandler<bool>? FullScreenChanged;

    public ICommand ClearDataCommand { get; }
    public ICommand BackCommand { get; }

    public SettingsViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;
        _settings = StorageService.Instance.LoadSettings();
        StorageUsageMb = StorageService.Instance.GetStorageUsageMb();

        ClearDataCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            var confirm = new ConfirmDialogWindow("Clear Saved Artwork", "Are you sure you want to delete all saved artwork and reset application data? This cannot be undone.");
            if (confirm.ShowDialog() == true)
            {
                StorageService.Instance.ClearAllUserData();
                StorageUsageMb = StorageService.Instance.GetStorageUsageMb();
                MessageBox.Show("All saved artwork has been cleared.", "Data Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Dashboard", null);
        });
    }

    private void SaveSettings()
    {
        StorageService.Instance.SaveSettings(_settings);
    }
}
