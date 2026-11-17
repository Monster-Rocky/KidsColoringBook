using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media.Imaging;
using KidsColoringBook.DrawingEngine;
using KidsColoringBook.Models;

namespace KidsColoringBook.Storage;

public class StorageService
{
    private static readonly Lazy<StorageService> _instance = new(() => new StorageService());
    public static StorageService Instance => _instance.Value;

    private readonly string _appDataFolder;
    private readonly string _drawingsFolder;
    private readonly string _settingsFilePath;
    private readonly string _drawingsCatalogFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public StorageService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _appDataFolder = Path.Combine(localAppData, "KidsColoringBook");
        _drawingsFolder = Path.Combine(_appDataFolder, "SavedDrawings");
        _settingsFilePath = Path.Combine(_appDataFolder, "settings.json");
        _drawingsCatalogFilePath = Path.Combine(_appDataFolder, "drawings.json");

        EnsureDirectories();
    }

    private void EnsureDirectories()
    {
        if (!Directory.Exists(_appDataFolder))
        {
            Directory.CreateDirectory(_appDataFolder);
        }
        if (!Directory.Exists(_drawingsFolder))
        {
            Directory.CreateDirectory(_drawingsFolder);
        }
    }

    // --- Settings Management ---
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch { }

        var defaults = new AppSettings();
        SaveSettings(defaults);
        return defaults;
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }

    // --- Sketches Catalog ---
    public List<SketchItem> LoadAllSketches()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string catalogPath = Path.Combine(baseDir, "Assets", "sketches.json");

        if (File.Exists(catalogPath))
        {
            try
            {
                string json = File.ReadAllText(catalogPath);
                var items = JsonSerializer.Deserialize<List<SketchItem>>(json);
                if (items != null && items.Count > 0)
                {
                    // Fix relative paths to absolute if needed
                    foreach (var item in items)
                    {
                        if (!Path.IsPathRooted(item.PngPath))
                            item.PngPath = Path.Combine(baseDir, item.PngPath);
                        if (!Path.IsPathRooted(item.SvgPath))
                            item.SvgPath = Path.Combine(baseDir, item.SvgPath);
                        if (!Path.IsPathRooted(item.ThumbnailPath))
                            item.ThumbnailPath = Path.Combine(baseDir, item.ThumbnailPath);
                    }
                    return items;
                }
            }
            catch { }
        }

        return new List<SketchItem>();
    }

    // --- User Artwork Gallery Management ---
    public List<UserDrawing> LoadUserDrawings()
    {
        try
        {
            if (File.Exists(_drawingsCatalogFilePath))
            {
                string json = File.ReadAllText(_drawingsCatalogFilePath);
                var list = JsonSerializer.Deserialize<List<UserDrawing>>(json);
                if (list != null)
                {
                    // Filter out any entries whose image file no longer exists
                    return list.Where(d => File.Exists(d.ImagePath)).OrderByDescending(d => d.ModifiedDate).ToList();
                }
            }
        }
        catch { }

        return new List<UserDrawing>();
    }

    public void SaveUserDrawing(UserDrawing drawing, WriteableBitmap bitmap)
    {
        EnsureDirectories();

        string imgFileName = $"art_{drawing.Id}.png";
        string thumbFileName = $"art_thumb_{drawing.Id}.png";
        string imgPath = Path.Combine(_drawingsFolder, imgFileName);
        string thumbPath = Path.Combine(_drawingsFolder, thumbFileName);

        // Save high-resolution PNG
        CanvasExporter.SaveToPng(bitmap, imgPath);
        // Save thumbnail PNG
        CanvasExporter.SaveThumbnail(bitmap, thumbPath, 200, 200);

        drawing.ImagePath = imgPath;
        drawing.ThumbnailPath = thumbPath;
        drawing.ModifiedDate = DateTime.Now;

        var existing = LoadUserDrawings();
        var idx = existing.FindIndex(d => d.Id == drawing.Id);
        if (idx >= 0)
        {
            existing[idx] = drawing;
        }
        else
        {
            existing.Insert(0, drawing);
        }

        SaveUserDrawingsCatalog(existing);
    }

    public void RenameUserDrawing(string id, string newTitle)
    {
        var list = LoadUserDrawings();
        var item = list.FirstOrDefault(d => d.Id == id);
        if (item != null)
        {
            item.Title = newTitle;
            item.ModifiedDate = DateTime.Now;
            SaveUserDrawingsCatalog(list);
        }
    }

    public void DeleteUserDrawing(string id)
    {
        var list = LoadUserDrawings();
        var item = list.FirstOrDefault(d => d.Id == id);
        if (item != null)
        {
            try
            {
                if (File.Exists(item.ImagePath)) File.Delete(item.ImagePath);
                if (File.Exists(item.ThumbnailPath)) File.Delete(item.ThumbnailPath);
            }
            catch { }

            list.Remove(item);
            SaveUserDrawingsCatalog(list);
        }
    }

    private void SaveUserDrawingsCatalog(List<UserDrawing> drawings)
    {
        try
        {
            string json = JsonSerializer.Serialize(drawings, _jsonOptions);
            File.WriteAllText(_drawingsCatalogFilePath, json);
        }
        catch { }
    }

    // --- Storage Info ---
    public double GetStorageUsageMb()
    {
        try
        {
            if (!Directory.Exists(_appDataFolder)) return 0;
            var di = new DirectoryInfo(_appDataFolder);
            long totalBytes = di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            return Math.Round((double)totalBytes / (1024 * 1024), 2);
        }
        catch
        {
            return 0;
        }
    }

    public void ClearAllUserData()
    {
        try
        {
            if (Directory.Exists(_drawingsFolder))
            {
                Directory.Delete(_drawingsFolder, true);
                Directory.CreateDirectory(_drawingsFolder);
            }
            if (File.Exists(_drawingsCatalogFilePath))
            {
                File.Delete(_drawingsCatalogFilePath);
            }
        }
        catch { }
    }
}
