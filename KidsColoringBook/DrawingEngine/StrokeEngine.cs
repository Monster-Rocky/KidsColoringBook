using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.DrawingEngine;

public enum DrawingTool
{
    Pencil,
    Fill,
    Eraser,
    Line,
    Rectangle,
    Circle,
    Star,
    Heart
}

/// <summary>
/// Advanced 2D drawing engine providing subpixel anti-aliased rendering,
/// smooth Bézier curve interpolation, rounded line caps and rounded joins.
/// Operates directly on WriteableBitmap unmanaged memory with maximum performance.
/// </summary>
public static class StrokeEngine
{
    /// <summary>
    /// Draws a single anti-aliased round dot at the specified coordinate.
    /// Perfect for single mouse clicks or taps.
    /// </summary>
    public static void DrawSmoothDot(WriteableBitmap bitmap, Point center, Color color, double thickness)
    {
        if (bitmap == null || thickness <= 0) return;

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        double radius = thickness / 2.0;
        double rInner = Math.Max(0.0, radius - 0.75);
        double rOuter = radius + 0.75;
        double rOuterSq = rOuter * rOuter;
        double invFeather = 1.0 / (rOuter - rInner);

        int minX = Math.Max(0, (int)Math.Floor(center.X - rOuter));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(center.X + rOuter));
        int minY = Math.Max(0, (int)Math.Floor(center.Y - rOuter));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(center.Y + rOuter));

        if (minX > maxX || minY > maxY) return;

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* pBuffer = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;
                byte b = color.B, g = color.G, r = color.R, a = color.A;

                for (int y = minY; y <= maxY; y++)
                {
                    double py = y + 0.5;
                    double dy = py - center.Y;
                    double dySq = dy * dy;
                    byte* row = pBuffer + y * stride;

                    for (int x = minX; x <= maxX; x++)
                    {
                        double px = x + 0.5;
                        double dx = px - center.X;
                        double distSq = dx * dx + dySq;

                        if (distSq < rOuterSq)
                        {
                            double dist = Math.Sqrt(distSq);
                            double alpha = (dist <= rInner) ? 1.0 : (rOuter - dist) * invFeather;
                            if (alpha > 1.0) alpha = 1.0;
                            else if (alpha < 0.0) alpha = 0.0;

                            int off = x * 4;
                            if (alpha >= 0.999 && a == 255)
                            {
                                row[off] = b;
                                row[off + 1] = g;
                                row[off + 2] = r;
                                row[off + 3] = 255;
                            }
                            else
                            {
                                double effA = alpha * (a / 255.0);
                                if (effA > 0.002)
                                {
                                    double inv = 1.0 - effA;
                                    row[off] = (byte)(b * effA + row[off] * inv);
                                    row[off + 1] = (byte)(g * effA + row[off + 1] * inv);
                                    row[off + 2] = (byte)(r * effA + row[off + 2] * inv);
                                    row[off + 3] = 255;
                                }
                            }
                        }
                    }
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    /// <summary>
    /// Draws a subpixel anti-aliased capsule segment with rounded caps between p1 and p2.
    /// Guarantees uniform line width at all angles and seamless rounded joins.
    /// </summary>
    public static void DrawCapsuleSegment(WriteableBitmap bitmap, Point p1, Point p2, Color color, double thickness)
    {
        if (bitmap == null || thickness <= 0) return;

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        double radius = thickness / 2.0;
        double rOuter = radius + 0.75;

        int minX = Math.Max(0, (int)Math.Floor(Math.Min(p1.X, p2.X) - rOuter));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(p1.X, p2.X) + rOuter));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(p1.Y, p2.Y) - rOuter));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(p1.Y, p2.Y) + rOuter));

        if (minX > maxX || minY > maxY) return;

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* pBuffer = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;
                DrawCapsuleRaw(pBuffer, stride, width, height, p1, p2, color, thickness);
            }

            bitmap.AddDirtyRect(new Int32Rect(minX, minY, maxX - minX + 1, maxY - minY + 1));
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    /// <summary>
    /// Smoothly draws a quadratic Bézier curve from pStart to pEnd using pCtrl as control point.
    /// Subdivides the curve into dense subpixel anti-aliased capsule segments with rounded joins.
    /// Updates a single unified dirty rect for optimal GPU performance.
    /// </summary>
    public static void DrawSmoothBezier(WriteableBitmap bitmap, Point pStart, Point pCtrl, Point pEnd, Color color, double thickness)
    {
        if (bitmap == null || thickness <= 0) return;

        int width = bitmap.PixelWidth;
        int height = bitmap.PixelHeight;

        double d1 = Math.Sqrt((pCtrl.X - pStart.X) * (pCtrl.X - pStart.X) + (pCtrl.Y - pStart.Y) * (pCtrl.Y - pStart.Y));
        double d2 = Math.Sqrt((pEnd.X - pCtrl.X) * (pEnd.X - pCtrl.X) + (pEnd.Y - pCtrl.Y) * (pEnd.Y - pCtrl.Y));
        double approxLen = d1 + d2;

        double stepSize = Math.Max(1.0, Math.Min(2.5, thickness * 0.25));
        int steps = (int)Math.Max(1, Math.Ceiling(approxLen / stepSize));

        double radius = thickness / 2.0;
        double rOuter = radius + 0.75;

        double bMinX = Math.Min(pStart.X, Math.Min(pCtrl.X, pEnd.X)) - rOuter;
        double bMaxX = Math.Max(pStart.X, Math.Max(pCtrl.X, pEnd.X)) + rOuter;
        double bMinY = Math.Min(pStart.Y, Math.Min(pCtrl.Y, pEnd.Y)) - rOuter;
        double bMaxY = Math.Max(pStart.Y, Math.Max(pCtrl.Y, pEnd.Y)) + rOuter;

        int dirtyMinX = Math.Max(0, (int)Math.Floor(bMinX));
        int dirtyMaxX = Math.Min(width - 1, (int)Math.Ceiling(bMaxX));
        int dirtyMinY = Math.Max(0, (int)Math.Floor(bMinY));
        int dirtyMaxY = Math.Min(height - 1, (int)Math.Ceiling(bMaxY));

        if (dirtyMinX > dirtyMaxX || dirtyMinY > dirtyMaxY) return;

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* pBuffer = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;

                Point prev = pStart;
                for (int i = 1; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    double u = 1.0 - t;
                    double currX = u * u * pStart.X + 2.0 * u * t * pCtrl.X + t * t * pEnd.X;
                    double currY = u * u * pStart.Y + 2.0 * u * t * pCtrl.Y + t * t * pEnd.Y;
                    Point curr = new Point(currX, currY);

                    DrawCapsuleRaw(pBuffer, stride, width, height, prev, curr, color, thickness);
                    prev = curr;
                }
            }

            bitmap.AddDirtyRect(new Int32Rect(dirtyMinX, dirtyMinY, dirtyMaxX - dirtyMinX + 1, dirtyMaxY - dirtyMinY + 1));
        }
        finally
        {
            bitmap.Unlock();
        }
    }

    /// <summary>
    /// Legacy entry point for drawing line segments; upgrades straight lines to anti-aliased capsules.
    /// </summary>
    public static void DrawLineSegment(WriteableBitmap bitmap, Point p1, Point p2, Color color, double thickness)
    {
        if (p1.X == p2.X && p1.Y == p2.Y)
        {
            DrawSmoothDot(bitmap, p1, color, thickness);
        }
        else
        {
            DrawCapsuleSegment(bitmap, p1, p2, color, thickness);
        }
    }

    /// <summary>
    /// Unmanaged inner core: renders an anti-aliased capsule directly into a locked pixel buffer.
    /// </summary>
    private static unsafe void DrawCapsuleRaw(byte* buffer, int stride, int width, int height,
        Point p1, Point p2, Color color, double thickness)
    {
        double radius = thickness / 2.0;
        double rInner = Math.Max(0.0, radius - 0.75);
        double rOuter = radius + 0.75;
        double rOuterSq = rOuter * rOuter;
        double invFeather = 1.0 / (rOuter - rInner);

        double vx = p2.X - p1.X;
        double vy = p2.Y - p1.Y;
        double lSq = vx * vx + vy * vy;

        int minX = Math.Max(0, (int)Math.Floor(Math.Min(p1.X, p2.X) - rOuter));
        int maxX = Math.Min(width - 1, (int)Math.Ceiling(Math.Max(p1.X, p2.X) + rOuter));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(p1.Y, p2.Y) - rOuter));
        int maxY = Math.Min(height - 1, (int)Math.Ceiling(Math.Max(p1.Y, p2.Y) + rOuter));

        if (minX > maxX || minY > maxY) return;

        byte b = color.B, g = color.G, r = color.R, a = color.A;

        for (int y = minY; y <= maxY; y++)
        {
            double py = y + 0.5;
            byte* row = buffer + y * stride;

            for (int x = minX; x <= maxX; x++)
            {
                double px = x + 0.5;

                // Project P(px, py) onto line segment P1-P2
                double t = 0.0;
                if (lSq > 0.0001)
                {
                    t = ((px - p1.X) * vx + (py - p1.Y) * vy) / lSq;
                    if (t < 0.0) t = 0.0;
                    else if (t > 1.0) t = 1.0;
                }

                double cx = p1.X + t * vx;
                double cy = p1.Y + t * vy;

                double dx = px - cx;
                double dy = py - cy;
                double distSq = dx * dx + dy * dy;

                if (distSq < rOuterSq)
                {
                    double dist = Math.Sqrt(distSq);
                    double alpha = (dist <= rInner) ? 1.0 : (rOuter - dist) * invFeather;
                    if (alpha > 1.0) alpha = 1.0;
                    else if (alpha < 0.0) alpha = 0.0;

                    int off = x * 4;
                    if (alpha >= 0.999 && a == 255)
                    {
                        row[off] = b;
                        row[off + 1] = g;
                        row[off + 2] = r;
                        row[off + 3] = 255;
                    }
                    else
                    {
                        double effA = alpha * (a / 255.0);
                        if (effA > 0.002)
                        {
                            double inv = 1.0 - effA;
                            row[off] = (byte)(b * effA + row[off] * inv);
                            row[off + 1] = (byte)(g * effA + row[off + 1] * inv);
                            row[off + 2] = (byte)(r * effA + row[off + 2] * inv);
                            row[off + 3] = 255;
                        }
                    }
                }
            }
        }
    }

    public static void DrawRectangle(WriteableBitmap bitmap, Point start, Point end, Color color, double thickness, bool filled = false)
    {
        int x1 = (int)Math.Min(start.X, end.X);
        int y1 = (int)Math.Min(start.Y, end.Y);
        int x2 = (int)Math.Max(start.X, end.X);
        int y2 = (int)Math.Max(start.Y, end.Y);

        if (!filled)
        {
            DrawCapsuleSegment(bitmap, new Point(x1, y1), new Point(x2, y1), color, thickness);
            DrawCapsuleSegment(bitmap, new Point(x2, y1), new Point(x2, y2), color, thickness);
            DrawCapsuleSegment(bitmap, new Point(x2, y2), new Point(x1, y2), color, thickness);
            DrawCapsuleSegment(bitmap, new Point(x1, y2), new Point(x1, y1), color, thickness);
        }
        else
        {
            FillRect(bitmap, x1, y1, x2 - x1, y2 - y1, color);
        }
    }

    public static void DrawEllipse(WriteableBitmap bitmap, Point start, Point end, Color color, double thickness)
    {
        double cx = (start.X + end.X) / 2.0;
        double cy = (start.Y + end.Y) / 2.0;
        double rx = Math.Abs(end.X - start.X) / 2.0;
        double ry = Math.Abs(end.Y - start.Y) / 2.0;

        if (rx < 1 || ry < 1) return;

        int numSteps = (int)Math.Max(48, Math.PI * (rx + ry) / 1.5);
        Point prev = new(cx + rx, cy);

        for (int i = 1; i <= numSteps; i++)
        {
            double angle = 2 * Math.PI * i / numSteps;
            Point current = new(cx + rx * Math.Cos(angle), cy + ry * Math.Sin(angle));
            DrawCapsuleSegment(bitmap, prev, current, color, thickness);
            prev = current;
        }
    }

    public static void DrawStar(WriteableBitmap bitmap, Point start, Point end, Color color, double thickness)
    {
        double cx = (start.X + end.X) / 2.0;
        double cy = (start.Y + end.Y) / 2.0;
        double outerR = Math.Min(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)) / 2.0;
        double innerR = outerR * 0.45;

        if (outerR < 2) return;

        int points = 5;
        Point[] pts = new Point[points * 2];
        for (int i = 0; i < points * 2; i++)
        {
            double r = (i % 2 == 0) ? outerR : innerR;
            double angle = i * Math.PI / points - Math.PI / 2.0;
            pts[i] = new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
        }

        for (int i = 0; i < pts.Length; i++)
        {
            Point pNext = pts[(i + 1) % pts.Length];
            DrawCapsuleSegment(bitmap, pts[i], pNext, color, thickness);
        }
    }

    public static void DrawHeart(WriteableBitmap bitmap, Point start, Point end, Color color, double thickness)
    {
        double cx = (start.X + end.X) / 2.0;
        double cy = (start.Y + end.Y) / 2.0;
        double size = Math.Min(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y)) / 2.0;
        if (size < 2) return;

        int numSteps = 96;
        Point prev = default;
        for (int i = 0; i <= numSteps; i++)
        {
            double t = 2 * Math.PI * i / numSteps;
            // Parametric heart curve
            double x = 16 * Math.Pow(Math.Sin(t), 3);
            double y = -(13 * Math.Cos(t) - 5 * Math.Cos(2 * t) - 2 * Math.Cos(3 * t) - Math.Cos(4 * t));
            Point current = new Point(cx + (x / 17.0) * size, cy + (y / 17.0) * size);
            if (i > 0)
            {
                DrawCapsuleSegment(bitmap, prev, current, color, thickness);
            }
            prev = current;
        }
    }

    public static void FillRect(WriteableBitmap bitmap, int x, int y, int w, int h, Color color)
    {
        if (bitmap == null || w <= 0 || h <= 0) return;

        int bw = bitmap.PixelWidth;
        int bh = bitmap.PixelHeight;

        int x1 = Math.Clamp(x, 0, bw - 1);
        int y1 = Math.Clamp(y, 0, bh - 1);
        int x2 = Math.Clamp(x + w, 0, bw - 1);
        int y2 = Math.Clamp(y + h, 0, bh - 1);

        bitmap.Lock();
        try
        {
            unsafe
            {
                byte* p = (byte*)bitmap.BackBuffer;
                int stride = bitmap.BackBufferStride;
                byte b = color.B, g = color.G, r = color.R, a = color.A;

                for (int cy = y1; cy <= y2; cy++)
                {
                    byte* row = p + cy * stride;
                    for (int cx = x1; cx <= x2; cx++)
                    {
                        int off = cx * 4;
                        row[off] = b;
                        row[off + 1] = g;
                        row[off + 2] = r;
                        row[off + 3] = a;
                    }
                }

                bitmap.AddDirtyRect(new Int32Rect(x1, y1, x2 - x1 + 1, y2 - y1 + 1));
            }
        }
        finally
        {
            bitmap.Unlock();
        }
    }
}
