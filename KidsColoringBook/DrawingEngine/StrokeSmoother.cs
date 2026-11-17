using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.DrawingEngine;

/// <summary>
/// Real-time stroke smoother using midpoint quadratic Bézier curve interpolation.
/// Converts discrete mouse/touch/stylus events into smooth C1-continuous curves
/// with anti-aliasing, rounded caps, and zero angular kinks.
/// </summary>
public class StrokeSmoother
{
    private Point _p0;       // Previous raw point (acts as Bézier control point)
    private Point _prevMid;  // Previous curve midpoint (curve anchor)
    private int _pointCount;
    private bool _isActive;

    public bool IsActive => _isActive;

    /// <summary>
    /// Starts a new stroke at the given coordinate.
    /// Draws an initial anti-aliased round dot for single taps or start of line.
    /// </summary>
    public void Start(WriteableBitmap bitmap, Point startPoint, Color color, double thickness)
    {
        if (bitmap == null) return;

        _p0 = startPoint;
        _prevMid = startPoint;
        _pointCount = 1;
        _isActive = true;

        // Render initial anti-aliased dot at starting point
        StrokeEngine.DrawSmoothDot(bitmap, startPoint, color, thickness);
    }

    /// <summary>
    /// Smoothly interpolates the stroke to the new point using quadratic Bézier curves.
    /// Suppresses subpixel jitter and generates dense subpixel curve segments.
    /// </summary>
    public void Move(WriteableBitmap bitmap, Point currentPoint, Color color, double thickness)
    {
        if (!_isActive || bitmap == null) return;

        double dx = currentPoint.X - _p0.X;
        double dy = currentPoint.Y - _p0.Y;
        double distSq = dx * dx + dy * dy;

        // Ignore micro-jitter (movements smaller than 1.0 pixel)
        if (distSq < 1.0) return;

        // Calculate midpoint between previous point and new point
        Point mid = new Point((_p0.X + currentPoint.X) / 2.0, (_p0.Y + currentPoint.Y) / 2.0);

        if (_pointCount == 1)
        {
            // Initial segment: from start point to first midpoint
            StrokeEngine.DrawCapsuleSegment(bitmap, _p0, mid, color, thickness);
        }
        else
        {
            // Smooth quadratic Bézier curve from previous midpoint to new midpoint
            StrokeEngine.DrawSmoothBezier(bitmap, _prevMid, _p0, mid, color, thickness);
        }

        _prevMid = mid;
        _p0 = currentPoint;
        _pointCount++;
    }

    /// <summary>
    /// Finalizes the stroke to the release coordinate.
    /// </summary>
    public void End(WriteableBitmap bitmap, Point? endPoint, Color color, double thickness)
    {
        if (!_isActive || bitmap == null) return;

        Point target = endPoint ?? _p0;

        if (_pointCount > 1)
        {
            // Connect the last midpoint to the final release point
            StrokeEngine.DrawCapsuleSegment(bitmap, _prevMid, target, color, thickness);
        }
        else if (_pointCount == 1 && endPoint.HasValue && (endPoint.Value.X != _p0.X || endPoint.Value.Y != _p0.Y))
        {
            StrokeEngine.DrawCapsuleSegment(bitmap, _p0, endPoint.Value, color, thickness);
        }

        _isActive = false;
        _pointCount = 0;
    }

    /// <summary>
    /// Cancels active stroke state without rendering further.
    /// </summary>
    public void Cancel()
    {
        _isActive = false;
        _pointCount = 0;
    }
}
