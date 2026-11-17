using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KidsColoringBook.ViewModels;

namespace KidsColoringBook.Views;

public partial class ColoringView : UserControl
{
    private bool _isDrawing = false;
    private Point _lastPoint;

    public ColoringView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            // Disable press-and-hold gestures and flicks on the drawing canvas to eliminate touch/stylus latency
            Stylus.SetIsPressAndHoldEnabled(SketchCanvasImage, false);
            Stylus.SetIsFlicksEnabled(SketchCanvasImage, false);
            Stylus.SetIsTapFeedbackEnabled(SketchCanvasImage, false);
            Stylus.SetIsTouchFeedbackEnabled(SketchCanvasImage, false);
        };
    }

    private ColoringViewModel? ViewModel => DataContext as ColoringViewModel;

    private Point? GetCanvasPixelCoordinate(Point screenPoint)
    {
        if (ViewModel?.CanvasBitmap == null) return null;

        double actualW = SketchCanvasImage.ActualWidth;
        double actualH = SketchCanvasImage.ActualHeight;

        if (actualW <= 0 || actualH <= 0) return null;

        double pixelX = (screenPoint.X / actualW) * ViewModel.CanvasBitmap.PixelWidth;
        double pixelY = (screenPoint.Y / actualH) * ViewModel.CanvasBitmap.PixelHeight;

        // Clamp to bitmap boundaries so fast sweeps do not lose edge coordinates
        pixelX = Math.Clamp(pixelX, 0, ViewModel.CanvasBitmap.PixelWidth - 1);
        pixelY = Math.Clamp(pixelY, 0, ViewModel.CanvasBitmap.PixelHeight - 1);

        return new Point(pixelX, pixelY);
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pt = e.GetPosition(SketchCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        SketchCanvasImage.CaptureMouse();

        ViewModel?.OnPointerDown(canvasPt.Value);
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetPosition(SketchCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        ViewModel?.OnPointerMove(canvasPt.Value);
        _lastPoint = canvasPt.Value;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing)
        {
            var pt = e.GetPosition(SketchCanvasImage);
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            SketchCanvasImage.ReleaseMouseCapture();
        }
    }

    private void OnCanvasTouchDown(object sender, TouchEventArgs e)
    {
        var pt = e.GetTouchPoint(SketchCanvasImage).Position;
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        SketchCanvasImage.CaptureTouch(e.TouchDevice);

        ViewModel?.OnPointerDown(canvasPt.Value);
        e.Handled = true;
    }

    private void OnCanvasTouchMove(object sender, TouchEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetTouchPoint(SketchCanvasImage).Position;
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        ViewModel?.OnPointerMove(canvasPt.Value);
        _lastPoint = canvasPt.Value;
        e.Handled = true;
    }

    private void OnCanvasTouchUp(object sender, TouchEventArgs e)
    {
        if (_isDrawing)
        {
            var pt = e.GetTouchPoint(SketchCanvasImage).Position;
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            SketchCanvasImage.ReleaseTouchCapture(e.TouchDevice);
        }
        e.Handled = true;
    }

    private void OnCanvasStylusDown(object sender, StylusDownEventArgs e)
    {
        var pt = e.GetPosition(SketchCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        SketchCanvasImage.CaptureStylus();

        ViewModel?.OnPointerDown(canvasPt.Value);
        e.Handled = true;
    }

    private void OnCanvasStylusMove(object sender, StylusEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetPosition(SketchCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        ViewModel?.OnPointerMove(canvasPt.Value);
        _lastPoint = canvasPt.Value;
        e.Handled = true;
    }

    private void OnCanvasStylusUp(object sender, StylusEventArgs e)
    {
        if (_isDrawing)
        {
            var pt = e.GetPosition(SketchCanvasImage);
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            SketchCanvasImage.ReleaseStylusCapture();
        }
        e.Handled = true;
    }
}
