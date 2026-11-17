using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.DrawingEngine;

public class HistoryManager
{
    private readonly int _maxSteps;
    private readonly Stack<byte[]> _undoStack = new();
    private readonly Stack<byte[]> _redoStack = new();
    private int _width;
    private int _height;
    private int _stride;

    public event EventHandler? HistoryChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public HistoryManager(int maxSteps = 20)
    {
        _maxSteps = maxSteps;
    }

    public void Initialize(WriteableBitmap bitmap)
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _width = bitmap.PixelWidth;
        _height = bitmap.PixelHeight;
        _stride = bitmap.BackBufferStride;
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public void PushState(WriteableBitmap bitmap)
    {
        if (bitmap == null) return;

        _width = bitmap.PixelWidth;
        _height = bitmap.PixelHeight;
        _stride = bitmap.BackBufferStride;

        int totalBytes = _stride * _height;
        byte[] snapshot = new byte[totalBytes];

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* src = (byte*)bitmap.BackBuffer;
                fixed (byte* dst = snapshot)
                {
                    Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
                }
            }
        }
        finally
        {
            bitmap.Unlock();
        }

        // Limit stack size
        if (_undoStack.Count >= _maxSteps)
        {
            var temp = new List<byte[]>(_undoStack);
            temp.RemoveAt(temp.Count - 1); // remove oldest
            _undoStack.Clear();
            for (int i = temp.Count - 1; i >= 0; i--)
            {
                _undoStack.Push(temp[i]);
            }
        }

        _undoStack.Push(snapshot);
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool Undo(WriteableBitmap targetBitmap)
    {
        if (!CanUndo || targetBitmap == null) return false;

        // Current state goes to redo stack
        byte[] current = ExtractBytes(targetBitmap);
        _redoStack.Push(current);

        byte[] previous = _undoStack.Pop();
        ApplyBytes(targetBitmap, previous);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool Redo(WriteableBitmap targetBitmap)
    {
        if (!CanRedo || targetBitmap == null) return false;

        // Current state goes to undo stack
        byte[] current = ExtractBytes(targetBitmap);
        _undoStack.Push(current);

        byte[] next = _redoStack.Pop();
        ApplyBytes(targetBitmap, next);

        HistoryChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private byte[] ExtractBytes(WriteableBitmap bitmap)
    {
        int totalBytes = bitmap.BackBufferStride * bitmap.PixelHeight;
        byte[] data = new byte[totalBytes];
        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* src = (byte*)bitmap.BackBuffer;
                fixed (byte* dst = data)
                {
                    Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
                }
            }
        }
        finally
        {
            bitmap.Unlock();
        }
        return data;
    }

    private void ApplyBytes(WriteableBitmap bitmap, byte[] data)
    {
        int totalBytes = bitmap.BackBufferStride * bitmap.PixelHeight;
        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* dst = (byte*)bitmap.BackBuffer;
                fixed (byte* src = data)
                {
                    Buffer.MemoryCopy(src, dst, totalBytes, totalBytes);
                }
            }
            bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
        }
        finally
        {
            bitmap.Unlock();
        }
    }
}
