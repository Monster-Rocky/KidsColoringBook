using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.DrawingEngine;

/// <summary>
/// High-performance scanline queue-based flood fill algorithm for WriteableBitmap.
/// Operates directly on the unmanaged pixel buffer via unsafe pointers.
/// Accurately respects dark outline boundaries and prevents color leaking.
/// </summary>
public static class FloodFillEngine
{
    // Threshold below which a pixel is considered part of the black outline boundary
    private const int OutlineLuminanceThreshold = 75;
    private const int ColorTolerance = 48; // Euclidean tolerance for anti-aliased fill matching

    public static bool Fill(WriteableBitmap bitmap, Point clickPoint, Color replacementColor)
    {
        if (bitmap == null) return false;

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        int startX = (int)Math.Round(clickPoint.X);
        int startY = (int)Math.Round(clickPoint.Y);

        if (startX < 0 || startX >= width || startY < 0 || startY >= height)
            return false;

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* pBackBuffer = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;

                // Bgra32: byte 0 = B, 1 = G, 2 = R, 3 = A
                int seedOffset = startY * stride + startX * 4;
                byte seedB = pBackBuffer[seedOffset];
                byte seedG = pBackBuffer[seedOffset + 1];
                byte seedR = pBackBuffer[seedOffset + 2];
                byte seedA = pBackBuffer[seedOffset + 3];

                // If clicking directly on a dark outline, do not fill
                int seedLuma = (seedR * 299 + seedG * 587 + seedB * 114) / 1000;
                if (seedLuma < OutlineLuminanceThreshold)
                {
                    return false;
                }

                byte fillR = replacementColor.R;
                byte fillG = replacementColor.G;
                byte fillB = replacementColor.B;
                byte fillA = replacementColor.A;

                // If target color is already virtually identical to fill color, skip
                if (ColorDistance(seedR, seedG, seedB, fillR, fillG, fillB) < 10)
                {
                    return false;
                }

                // Visited / processed mask to prevent infinite loops and re-processing
                var visited = new bool[width * height];
                var queue = new Queue<Point2D>();
                queue.Enqueue(new Point2D(startX, startY));
                visited[startY * width + startX] = true;

                int minX = startX, maxX = startX, minY = startY, maxY = startY;

                // Scanline fill
                while (queue.Count > 0)
                {
                    var pt = queue.Dequeue();
                    int px = pt.X;
                    int py = pt.Y;

                    // Extend to the left
                    int leftX = px;
                    while (leftX > 0)
                    {
                        int checkX = leftX - 1;
                        int checkIndex = py * width + checkX;
                        if (visited[checkIndex]) break;

                        int off = py * stride + checkX * 4;
                        byte cb = pBackBuffer[off];
                        byte cg = pBackBuffer[off + 1];
                        byte cr = pBackBuffer[off + 2];

                        if (!IsFillable(cr, cg, cb, seedR, seedG, seedB))
                            break;

                        leftX--;
                        visited[checkIndex] = true;
                    }

                    // Extend to the right
                    int rightX = px;
                    while (rightX < width - 1)
                    {
                        int checkX = rightX + 1;
                        int checkIndex = py * width + checkX;
                        if (visited[checkIndex]) break;

                        int off = py * stride + checkX * 4;
                        byte cb = pBackBuffer[off];
                        byte cg = pBackBuffer[off + 1];
                        byte cr = pBackBuffer[off + 2];

                        if (!IsFillable(cr, cg, cb, seedR, seedG, seedB))
                            break;

                        rightX++;
                        visited[checkIndex] = true;
                    }

                    // Fill current span
                    for (int x = leftX; x <= rightX; x++)
                    {
                        int off = py * stride + x * 4;
                        pBackBuffer[off] = fillB;
                        pBackBuffer[off + 1] = fillG;
                        pBackBuffer[off + 2] = fillR;
                        pBackBuffer[off + 3] = fillA;
                    }

                    if (leftX < minX) minX = leftX;
                    if (rightX > maxX) maxX = rightX;
                    if (py < minY) minY = py;
                    if (py > maxY) maxY = py;

                    // Inspect rows above and below
                    ScanRow(leftX, rightX, py - 1, width, height, stride, pBackBuffer, seedR, seedG, seedB, visited, queue);
                    ScanRow(leftX, rightX, py + 1, width, height, stride, pBackBuffer, seedR, seedG, seedB, visited, queue);
                }

                // Add dirty rect for updated region
                bitmap.AddDirtyRect(new Int32Rect(minX, minY, (maxX - minX + 1), (maxY - minY + 1)));
                return true;
            }
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    private static unsafe void ScanRow(int leftX, int rightX, int y, int width, int height, int stride,
        byte* pBackBuffer, byte seedR, byte seedG, byte seedB, bool[] visited, Queue<Point2D> queue)
    {
        if (y < 0 || y >= height) return;

        bool inSpan = false;
        for (int x = leftX; x <= rightX; x++)
        {
            int index = y * width + x;
            if (visited[index])
            {
                inSpan = false;
                continue;
            }

            int off = y * stride + x * 4;
            byte cb = pBackBuffer[off];
            byte cg = pBackBuffer[off + 1];
            byte cr = pBackBuffer[off + 2];

            if (IsFillable(cr, cg, cb, seedR, seedG, seedB))
            {
                if (!inSpan)
                {
                    queue.Enqueue(new Point2D(x, y));
                    visited[index] = true;
                    inSpan = true;
                }
            }
            else
            {
                inSpan = false;
            }
        }
    }

    private static bool IsFillable(byte r, byte g, byte b, byte seedR, byte seedG, byte seedB)
    {
        // Outlines are dark pixels: never fill across an outline
        int luma = (r * 299 + g * 587 + b * 114) / 1000;
        if (luma < OutlineLuminanceThreshold)
            return false;

        // Check color distance to seed pixel
        return ColorDistance(r, g, b, seedR, seedG, seedB) <= ColorTolerance;
    }

    private static int ColorDistance(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        return (int)Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    private readonly struct Point2D
    {
        public readonly int X;
        public readonly int Y;
        public Point2D(int x, int y) { X = x; Y = y; }
    }
}
