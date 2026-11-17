using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.DrawingEngine;

public static class CanvasExporter
{
    public static void SaveToPng(BitmapSource bitmap, string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }

    public static BitmapSource CreateThumbnail(BitmapSource source, int targetWidth = 200, int targetHeight = 200)
    {
        double scaleX = (double)targetWidth / source.PixelWidth;
        double scaleY = (double)targetHeight / source.PixelHeight;

        var transformed = new TransformedBitmap(source, new ScaleTransform(scaleX, scaleY));
        return transformed;
    }

    public static void SaveThumbnail(BitmapSource source, string thumbnailPath, int targetWidth = 200, int targetHeight = 200)
    {
        var thumb = CreateThumbnail(source, targetWidth, targetHeight);
        SaveToPng(thumb, thumbnailPath);
    }

    public static WriteableBitmap CreateBlankCanvas(int width = 800, int height = 800, Color? initialColor = null)
    {
        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        Color fill = initialColor ?? Colors.White;
        StrokeEngine.FillRect(bitmap, 0, 0, width, height, fill);
        return bitmap;
    }

    public static WriteableBitmap LoadFromPng(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        // Ensure Bgra32 format
        var formatted = new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        var writeable = new WriteableBitmap(formatted);
        return writeable;
    }

    public static WriteableBitmap CloneWriteableBitmap(WriteableBitmap source)
    {
        var clone = new WriteableBitmap(source.PixelWidth, source.PixelHeight, source.DpiX, source.DpiY, source.Format, null);
        int totalBytes = source.BackBufferStride * source.PixelHeight;

        source.Lock();
        clone.Lock();
        try
        {
            unsafe
            {
                byte* src = (byte*)source.BackBuffer;
                byte* dst = (byte*)clone.BackBuffer;
                Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
            }
            clone.AddDirtyRect(new Int32Rect(0, 0, clone.PixelWidth, clone.PixelHeight));
        }
        finally
        {
            source.Unlock();
            clone.Unlock();
        }
        return clone;
    }
}
