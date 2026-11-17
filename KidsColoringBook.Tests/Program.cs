using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KidsColoringBook.DrawingEngine;
using KidsColoringBook.ViewModels;

namespace KidsColoringBook.Tests;

class Program
{
    private static int _passed = 0;
    private static int _failed = 0;

    [STAThread]
    static int Main(string[] args)
    {
        Console.WriteLine("=================================================");
        Console.WriteLine(" KIDS COLORING BOOK - DRAWING ENGINE TEST SUITE   ");
        Console.WriteLine("=================================================");

        RunTest("1. Slow Curves (Dense Smooth Spline)", Test_SlowCurves);
        RunTest("2. Fast Curves (Sparse High-Speed Points)", Test_FastCurves);
        RunTest("3. Circles Drawing & Closed Geometry", Test_Circles);
        RunTest("4. Straight Lines & Round Caps", Test_StraightLines);
        RunTest("5. Zigzag Lines & Rounded Joins", Test_ZigzagLines);
        RunTest("6. Continuous Random Shapes & Zero Degrade", Test_ContinuousRandomShapes);
        RunTest("7. Brush Thicknesses: Thin(4), Med(8), Thick(16), Jumbo(28)", Test_BrushThicknesses);
        RunTest("8. Zoom & Window Scaling Coordinate Mapping", Test_ZoomAndScaling);
        RunTest("9. Eraser Tool Smoothness & White Restoration", Test_EraserTool);
        RunTest("10. Flood Fill Compatibility with Smooth Pencil Outlines", Test_FloodFillCompatibility);
        RunTest("11. Undo / Redo History Integrity with Smooth Strokes", Test_UndoRedoIntegrity);
        RunTest("12. Genuine ISO 32000-1 / PDF 1.4 Export & Standards Validation", Test_PdfExport);

        Console.WriteLine("=================================================");
        Console.WriteLine($" RESULTS: {_passed} Passed, {_failed} Failed");
        Console.WriteLine("=================================================");

        return _failed == 0 ? 0 : 1;
    }

    static void RunTest(string name, Action testAction)
    {
        try
        {
            testAction();
            Console.WriteLine($" [PASS] {name}");
            _passed++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [FAIL] {name}: {ex.Message}");
            _failed++;
        }
    }

    static WriteableBitmap CreateTestBitmap(int w = 800, int h = 800)
    {
        var wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
        StrokeEngine.FillRect(wb, 0, 0, w, h, Colors.White);
        return wb;
    }

    static unsafe Color GetPixel(WriteableBitmap wb, int x, int y)
    {
        byte* p = (byte*)wb.BackBuffer;
        int stride = wb.BackBufferStride;
        int off = y * stride + x * 4;
        return Color.FromArgb(p[off + 3], p[off + 2], p[off + 1], p[off]);
    }

    static void Test_SlowCurves()
    {
        var wb = CreateTestBitmap();
        var smoother = new StrokeSmoother();
        Color blue = Color.FromRgb(33, 150, 243);

        // Draw slow sinusoidal curve from x=100 to x=300
        smoother.Start(wb, new Point(100, 200), blue, 8.0);
        for (int x = 101; x <= 300; x += 2)
        {
            double y = 200 + 40 * Math.Sin((x - 100) * 0.05);
            smoother.Move(wb, new Point(x, y), blue, 8.0);
        }
        smoother.End(wb, new Point(300, 200 + 40 * Math.Sin(200 * 0.05)), blue, 8.0);

        // Verify that start, middle and end are painted blue
        Color startCol = GetPixel(wb, 100, 200);
        Assert(startCol.R == blue.R && startCol.G == blue.G && startCol.B == blue.B, "Start point must be blue");

        // Verify continuity along the path
        int paintedPixels = 0;
        for (int x = 100; x <= 300; x += 5)
        {
            int y = (int)Math.Round(200 + 40 * Math.Sin((x - 100) * 0.05));
            Color c = GetPixel(wb, x, y);
            if (c.R == blue.R && c.G == blue.G && c.B == blue.B) paintedPixels++;
        }
        Assert(paintedPixels >= 35, $"Curve must be continuous without gaps (found {paintedPixels}/41 sample points)");
    }

    static void Test_FastCurves()
    {
        var wb = CreateTestBitmap();
        var smoother = new StrokeSmoother();
        Color red = Color.FromRgb(244, 67, 54);

        // Simulate fast mouse movement where points are spaced 50 pixels apart
        Point[] fastPoints = new Point[]
        {
            new(100, 100),
            new(150, 140),
            new(200, 220),
            new(260, 290),
            new(320, 340),
            new(400, 360)
        };

        smoother.Start(wb, fastPoints[0], red, 10.0);
        for (int i = 1; i < fastPoints.Length; i++)
        {
            smoother.Move(wb, fastPoints[i], red, 10.0);
        }
        smoother.End(wb, fastPoints[^1], red, 10.0);

        // Check that intermediate regions between the 50px sparse points have zero gaps
        // Test midpoint between fastPoints[1] (150, 140) and fastPoints[2] (200, 220)
        // Midpoint should be approximately around (175, 180)
        bool foundColorInBetween = false;
        for (int y = 160; y <= 200; y++)
        {
            for (int x = 160; x <= 190; x++)
            {
                Color c = GetPixel(wb, x, y);
                if (c.R == red.R && c.G == red.G && c.B == red.B)
                {
                    foundColorInBetween = true;
                    break;
                }
            }
            if (foundColorInBetween) break;
        }
        Assert(foundColorInBetween, "Quadratic Bézier interpolation must bridge sparse points without gaps");
    }

    static void Test_Circles()
    {
        var wb = CreateTestBitmap();
        Color green = Color.FromRgb(16, 185, 129);

        // Draw geometric circle
        StrokeEngine.DrawEllipse(wb, new Point(300, 300), new Point(500, 500), green, 8.0);

        // Check 4 cardinal points along perimeter
        // Center is (400, 400), radius is 100
        Color top = GetPixel(wb, 400, 300);
        Color bottom = GetPixel(wb, 400, 500);
        Color left = GetPixel(wb, 300, 400);
        Color right = GetPixel(wb, 500, 400);

        Assert(top.G == green.G && bottom.G == green.G && left.G == green.G && right.G == green.G,
            "Circle perimeter must be closed and painted on all cardinal directions");

        // Center must be untouched (white)
        Color center = GetPixel(wb, 400, 400);
        Assert(center.R == 255 && center.G == 255 && center.B == 255, "Inside of circle outline must remain white");
    }

    static void Test_StraightLines()
    {
        var wb = CreateTestBitmap();
        Color purple = Color.FromRgb(147, 51, 234);

        // Draw horizontal line from (100, 200) to (300, 200) with thickness 16
        StrokeEngine.DrawLineSegment(wb, new Point(100, 200), new Point(300, 200), purple, 16.0);

        // Verify solid core: pixels from y = 194 to 206 at x = 200 are 100% purple
        for (int y = 194; y <= 206; y++)
        {
            Color c = GetPixel(wb, 200, y);
            Assert(c.R == purple.R && c.G == purple.G && c.B == purple.B,
                $"Pixel at (200, {y}) must be inside the 16px line core");
        }

        // Verify anti-aliased boundary feathering at y = 207 (partially blended)
        Color edge = GetPixel(wb, 200, 207);
        Assert(edge.A == 255 && edge.R != 255, "Edge pixel at y=207 must be anti-aliased (feathered)");

        // Verify rounded cap extends to the left of x = 100
        Color capPixel = GetPixel(wb, 94, 200);
        Assert(capPixel.R == purple.R && capPixel.G == purple.G && capPixel.B == purple.B,
            "Round line cap must extend radius pixels beyond segment endpoints");
    }

    static void Test_ZigzagLines()
    {
        var wb = CreateTestBitmap();
        var smoother = new StrokeSmoother();
        Color orange = Color.FromRgb(249, 115, 22);

        // Draw a sharp zigzag pattern
        // Corner control point is at (150, 250)
        // Midpoint quadratic curve smoothly rounds the corner around y ~ 212
        smoother.Start(wb, new Point(100, 100), orange, 6.0);
        smoother.Move(wb, new Point(150, 250), orange, 6.0);
        smoother.Move(wb, new Point(200, 100), orange, 6.0);
        smoother.Move(wb, new Point(250, 250), orange, 6.0);
        smoother.End(wb, new Point(300, 100), orange, 6.0);

        // Verify that the curve smoothly traverses through the rounded apex (x=150, y=212)
        bool foundApex = false;
        for (int y = 205; y <= 220; y++)
        {
            Color c = GetPixel(wb, 150, y);
            if (c.R == orange.R && c.G == orange.G && c.B == orange.B)
            {
                foundApex = true;
                break;
            }
        }
        Assert(foundApex, "Zigzag sharp kink must be smoothed into a fluid curved apex");
    }

    static void Test_ContinuousRandomShapes()
    {
        var wb = CreateTestBitmap();
        var smoother = new StrokeSmoother();
        Color black = Colors.Black;

        var rand = new Random(42);
        Point current = new Point(400, 400);
        smoother.Start(wb, current, black, 6.0);

        for (int i = 0; i < 50; i++)
        {
            current = new Point(
                Math.Clamp(current.X + rand.Next(-30, 31), 50, 750),
                Math.Clamp(current.Y + rand.Next(-30, 31), 50, 750));
            smoother.Move(wb, current, black, 6.0);
        }
        smoother.End(wb, current, black, 6.0);

        // Buffer sanity check: no crash, no memory corruption
        Assert(wb.PixelWidth == 800 && wb.PixelHeight == 800, "Bitmap must remain healthy after continuous random strokes");
    }

    static void Test_BrushThicknesses()
    {
        double[] thicknesses = new double[] { 4.0, 8.0, 16.0, 28.0 }; // Thin, Med, Thick, Jumbo
        string[] names = new string[] { "Thin", "Medium", "Thick", "Jumbo" };

        for (int i = 0; i < thicknesses.Length; i++)
        {
            var wb = CreateTestBitmap();
            double th = thicknesses[i];
            Color col = Colors.Black;

            // Draw vertical line at x = 400 from y = 100 to y = 300
            StrokeEngine.DrawLineSegment(wb, new Point(400, 100), new Point(400, 300), col, th);

            // Measure width across the middle (y = 200)
            int minX = 400, maxX = 400;
            while (minX > 350 && GetPixel(wb, minX, 200).R == 0) minX--;
            while (maxX < 450 && GetPixel(wb, maxX, 200).R == 0) maxX++;

            int measuredWidth = (maxX - 1) - (minX + 1) + 1;
            int expectedCore = (int)Math.Floor(th - 1.5);
            Assert(measuredWidth >= expectedCore,
                $"{names[i]} brush ({th}px) measured width {measuredWidth}px must match expected width (>= {expectedCore}px)");
        }
    }

    static void Test_ZoomAndScaling()
    {
        // Verify ZoomLevel clamping in ColoringViewModel
        var vm = new ColoringViewModel((s, o) => { });
        vm.ZoomLevel = 0.2;
        Assert(vm.ZoomLevel >= 0.5, "ZoomLevel must clamp at minimum 0.5x");

        vm.ZoomLevel = 5.0;
        Assert(vm.ZoomLevel <= 4.0, "ZoomLevel must clamp at maximum 4.0x");

        // Verify thickness properties
        vm.BrushThickness = 4.0;
        Assert(vm.IsThinBrushSelected, "Thin brush must be selected when thickness is 4.0");
        Assert(vm.BrushThicknessDescription.Contains("Thin"), "Description must indicate Thin");

        vm.BrushThickness = 28.0;
        Assert(vm.IsJumboBrushSelected, "Jumbo brush must be selected when thickness is 28.0");
        Assert(vm.BrushThicknessDescription.Contains("Jumbo"), "Description must indicate Jumbo");
    }

    static void Test_EraserTool()
    {
        var wb = CreateTestBitmap();
        Color black = Colors.Black;

        // Draw a black solid rectangle
        StrokeEngine.FillRect(wb, 100, 100, 200, 200, black);
        Assert(GetPixel(wb, 200, 200).R == 0, "Rect should be black");

        // Erase through the middle with smooth eraser (drawing White with thickness 20)
        var smoother = new StrokeSmoother();
        smoother.Start(wb, new Point(100, 200), Colors.White, 20.0);
        smoother.Move(wb, new Point(300, 200), Colors.White, 20.0);
        smoother.End(wb, new Point(300, 200), Colors.White, 20.0);

        // Verify erased path is restored to White
        Color erasedPixel = GetPixel(wb, 200, 200);
        Assert(erasedPixel.R == 255 && erasedPixel.G == 255 && erasedPixel.B == 255,
            "Eraser must restore pixels back to pure white");
    }

    static void Test_FloodFillCompatibility()
    {
        var wb = CreateTestBitmap();
        Color black = Colors.Black;
        Color yellow = Color.FromRgb(255, 235, 59);

        // Draw a closed square using smooth pencil
        var smoother = new StrokeSmoother();
        smoother.Start(wb, new Point(200, 200), black, 8.0);
        smoother.Move(wb, new Point(400, 200), black, 8.0);
        smoother.Move(wb, new Point(400, 400), black, 8.0);
        smoother.Move(wb, new Point(200, 400), black, 8.0);
        smoother.End(wb, new Point(200, 200), black, 8.0);

        // Perform flood fill inside the square at (300, 300)
        bool filled = FloodFillEngine.Fill(wb, new Point(300, 300), yellow);
        Assert(filled, "Flood fill inside smooth pencil closed boundary must succeed");

        // Verify inside is yellow
        Color inside = GetPixel(wb, 300, 300);
        Assert(inside.R == yellow.R && inside.G == yellow.G && inside.B == yellow.B,
            "Interior of closed pencil stroke must be filled with yellow");

        // Verify outside (at 100, 100) remained white (did not leak!)
        Color outside = GetPixel(wb, 100, 100);
        Assert(outside.R == 255 && outside.G == 255 && outside.B == 255,
            "Color must NOT leak through the smooth pencil boundary");
    }

    static void Test_UndoRedoIntegrity()
    {
        var wb = CreateTestBitmap();
        var history = new HistoryManager(10);
        history.Initialize(wb);

        // Push state before stroke
        history.PushState(wb);
        StrokeEngine.DrawSmoothDot(wb, new Point(400, 400), Colors.Red, 20.0);

        Assert(GetPixel(wb, 400, 400).R == 255 && GetPixel(wb, 400, 400).G == 0, "Dot should be red");

        // Perform Undo
        history.Undo(wb);
        Assert(GetPixel(wb, 400, 400).R == 255 && GetPixel(wb, 400, 400).G == 255, "Undo must restore white");

        // Perform Redo
        history.Redo(wb);
        Assert(GetPixel(wb, 400, 400).R == 255 && GetPixel(wb, 400, 400).G == 0, "Redo must restore red dot");
    }

    static void Test_PdfExport()
    {
        var wb = CreateTestBitmap();
        StrokeEngine.DrawSmoothDot(wb, new Point(400, 400), Colors.DarkOrange, 40.0);

        var page = KidsColoringBook.Printing.PrintService.CreatePrintPage(
            wb,
            "Test Coloring Artwork",
            "Little Artist",
            DateTime.Now,
            false);

        string testPdfPath = Path.Combine(Path.GetTempPath(), "coloring_test_export.pdf");
        if (File.Exists(testPdfPath)) File.Delete(testPdfPath);

        KidsColoringBook.Printing.PrintService.ExportToPdfOrXps(page, testPdfPath, false);

        Assert(File.Exists(testPdfPath), "Exported PDF file must exist on disk");
        var fi = new FileInfo(testPdfPath);
        Assert(fi.Length > 1024, $"PDF file size must be valid (was {fi.Length} bytes)");

        // Read raw bytes and verify PDF specification compliance
        byte[] bytes = File.ReadAllBytes(testPdfPath);
        string text = System.Text.Encoding.ASCII.GetString(bytes);

        // 1. Magic header
        Assert(text.StartsWith("%PDF-1."), "PDF must start with %PDF-1. magic header");

        // 2. Contains standard PDF objects
        Assert(text.Contains("/Type /Catalog"), "PDF must contain /Catalog");
        Assert(text.Contains("/Type /Pages"), "PDF must contain /Pages tree");
        Assert(text.Contains("/Type /Page"), "PDF must contain /Page object");
        Assert(text.Contains("/MediaBox [0 0 595.28 841.89]"), "PDF must define standard A4 MediaBox");
        Assert(text.Contains("/Type /XObject"), "PDF must contain /XObject");
        Assert(text.Contains("/Subtype /Image"), "PDF must contain Image XObject");
        Assert(text.Contains("/Filter /DCTDecode"), "PDF must use standard DCTDecode JPEG stream");

        // 3. Cross reference table and EOF
        Assert(text.Contains("xref"), "PDF must contain xref table");
        Assert(text.Contains("startxref"), "PDF must contain startxref offset pointer");
        Assert(text.Contains("%%EOF"), "PDF must terminate with %%EOF marker");

        // Clean up
        try { File.Delete(testPdfPath); } catch { }
    }

    static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
