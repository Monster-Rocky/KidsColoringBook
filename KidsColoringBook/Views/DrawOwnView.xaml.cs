using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KidsColoringBook.ViewModels;

namespace KidsColoringBook.Views;

public partial class DrawOwnView : UserControl
{
    private bool _isDrawing = false;
    private Point _lastPoint;

    public DrawOwnView()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            // Disable press-and-hold gestures and flicks to eliminate touch/stylus latency
            Stylus.SetIsPressAndHoldEnabled(DrawCanvasImage, false);
            Stylus.SetIsFlicksEnabled(DrawCanvasImage, false);
            Stylus.SetIsTapFeedbackEnabled(DrawCanvasImage, false);
            Stylus.SetIsTouchFeedbackEnabled(DrawCanvasImage, false);
        };
    }

    private DrawOwnViewModel? ViewModel => DataContext as DrawOwnViewModel;

    private Point? GetCanvasPixelCoordinate(Point screenPoint)
    {
        if (ViewModel?.CanvasBitmap == null) return null;

        double actualW = DrawCanvasImage.ActualWidth;
        double actualH = DrawCanvasImage.ActualHeight;

        if (actualW <= 0 || actualH <= 0) return null;

        double pixelX = (screenPoint.X / actualW) * ViewModel.CanvasBitmap.PixelWidth;
        double pixelY = (screenPoint.Y / actualH) * ViewModel.CanvasBitmap.PixelHeight;

        pixelX = Math.Clamp(pixelX, 0, ViewModel.CanvasBitmap.PixelWidth - 1);
        pixelY = Math.Clamp(pixelY, 0, ViewModel.CanvasBitmap.PixelHeight - 1);

        return new Point(pixelX, pixelY);
    }

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pt = e.GetPosition(DrawCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        DrawCanvasImage.CaptureMouse();

        ViewModel?.OnPointerDown(canvasPt.Value);
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetPosition(DrawCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        ViewModel?.OnPointerMove(canvasPt.Value);
        _lastPoint = canvasPt.Value;
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing)
        {
            var pt = e.GetPosition(DrawCanvasImage);
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            DrawCanvasImage.ReleaseMouseCapture();
        }
    }

    private void OnCanvasTouchDown(object sender, TouchEventArgs e)
    {
        var pt = e.GetTouchPoint(DrawCanvasImage).Position;
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        DrawCanvasImage.CaptureTouch(e.TouchDevice);

        ViewModel?.OnPointerDown(canvasPt.Value);
        e.Handled = true;
    }

    private void OnCanvasTouchMove(object sender, TouchEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetTouchPoint(DrawCanvasImage).Position;
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
            var pt = e.GetTouchPoint(DrawCanvasImage).Position;
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            DrawCanvasImage.ReleaseTouchCapture(e.TouchDevice);
        }
        e.Handled = true;
    }

    private void OnCanvasStylusDown(object sender, StylusDownEventArgs e)
    {
        var pt = e.GetPosition(DrawCanvasImage);
        var canvasPt = GetCanvasPixelCoordinate(pt);
        if (canvasPt == null) return;

        _isDrawing = true;
        _lastPoint = canvasPt.Value;
        DrawCanvasImage.CaptureStylus();

        ViewModel?.OnPointerDown(canvasPt.Value);
        e.Handled = true;
    }

    private void OnCanvasStylusMove(object sender, StylusEventArgs e)
    {
        if (!_isDrawing) return;

        var pt = e.GetPosition(DrawCanvasImage);
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
            var pt = e.GetPosition(DrawCanvasImage);
            var canvasPt = GetCanvasPixelCoordinate(pt);
            ViewModel?.OnPointerUp(canvasPt ?? _lastPoint);

            _isDrawing = false;
            DrawCanvasImage.ReleaseStylusCapture();
        }
        e.Handled = true;
    }
}
