using System;
using System.IO;
using System.Media;
using KidsColoringBook.Storage;

namespace KidsColoringBook.Services;

public class SoundService
{
    private static readonly Lazy<SoundService> _instance = new(() => new SoundService());
    public static SoundService Instance => _instance.Value;

    public bool IsSoundEnabled { get; set; } = true;

    public SoundService()
    {
        var settings = StorageService.Instance.LoadSettings();
        IsSoundEnabled = settings.SoundEffectsEnabled;
    }

    public void PlayClick()
    {
        if (!IsSoundEnabled) return;
        try
        {
            SystemSounds.Beep.Play();
        }
        catch { }
    }

    public void PlayPop()
    {
        if (!IsSoundEnabled) return;
        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch { }
    }

    public void PlaySuccess()
    {
        if (!IsSoundEnabled) return;
        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch { }
    }
}
