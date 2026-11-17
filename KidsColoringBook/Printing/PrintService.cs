using System;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.Printing;

public class PrintService
{
    // A4 dimensions in 96 DPI
    public const double A4Width = 794.0;
    public const double A4Height = 1123.0;

    public static FrameworkElement CreatePrintPage(
        BitmapSource artworkImage,
        string artworkTitle,
        string childName,
        DateTime date,
        bool isLandscape = false)
    {
        double pageWidth = isLandscape ? A4Height : A4Width;
        double pageHeight = isLandscape ? A4Width : A4Height;

        var pageContainer = new Border
        {
            Width = pageWidth,
            Height = pageHeight,
            Background = Brushes.White,
            Padding = new Thickness(40)
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header / Title
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Sketch Image
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer Signature

        // Header Title
        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 0, 0, 15),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var titleText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(artworkTitle) ? "Kids Coloring Book" : artworkTitle,
            FontSize = 26,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = new FontFamily("Comic Sans MS, Segoe UI, Arial")
        };
        headerPanel.Children.Add(titleText);
        Grid.SetRow(headerPanel, 0);
        mainGrid.Children.Add(headerPanel);

        // Artwork Frame
        var imageBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Background = Brushes.White,
            Padding = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var imageControl = new Image
        {
            Source = artworkImage,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        imageBorder.Child = imageControl;
        Grid.SetRow(imageBorder, 1);
        mainGrid.Children.Add(imageBorder);

        // Footer: Child Signature & Date
        var footerBorder = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
            BorderThickness = new Thickness(0, 2, 0, 0),
            Padding = new Thickness(0, 15, 0, 0),
            Margin = new Thickness(0, 15, 0, 0)
        };

        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var createdByPanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        createdByPanel.Children.Add(new TextBlock
        {
            Text = "Created By: ",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            FontFamily = new FontFamily("Comic Sans MS, Segoe UI")
        });
        createdByPanel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(childName) ? "Little Artist" : childName,
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0)), // Child-friendly warm orange
            FontFamily = new FontFamily("Comic Sans MS, Segoe UI")
        });
        Grid.SetColumn(createdByPanel, 0);
        footerGrid.Children.Add(createdByPanel);

        var datePanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        datePanel.Children.Add(new TextBlock
        {
            Text = "Date: ",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            FontFamily = new FontFamily("Comic Sans MS, Segoe UI")
        });
        datePanel.Children.Add(new TextBlock
        {
            Text = date.ToString("dd/MM/yyyy"),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
            FontFamily = new FontFamily("Comic Sans MS, Segoe UI")
        });
        Grid.SetColumn(datePanel, 1);
        footerGrid.Children.Add(datePanel);

        footerBorder.Child = footerGrid;
        Grid.SetRow(footerBorder, 2);
        mainGrid.Children.Add(footerBorder);

        pageContainer.Child = mainGrid;

        // Force layout pass
        pageContainer.Measure(new Size(pageWidth, pageHeight));
        pageContainer.Arrange(new Rect(0, 0, pageWidth, pageHeight));
        pageContainer.UpdateLayout();

        return pageContainer;
    }

    public static RenderTargetBitmap RenderPageToBitmap(FrameworkElement pageElement, double width, double height)
    {
        pageElement.Measure(new Size(width, height));
        pageElement.Arrange(new Rect(0, 0, width, height));
        pageElement.UpdateLayout();

        var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(pageElement);
        return rtb;
    }

    public static bool Print(FrameworkElement pageElement, string jobTitle = "Kids Coloring Book", bool isLandscape = false)
    {
        var printDlg = new PrintDialog();
        if (printDlg.ShowDialog() == true)
        {
            try
            {
                if (isLandscape)
                {
                    printDlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
                }
                else
                {
                    printDlg.PrintTicket.PageOrientation = PageOrientation.Portrait;
                }

                double printableW = printDlg.PrintableAreaWidth;
                double printableH = printDlg.PrintableAreaHeight;

                pageElement.Measure(new Size(printableW, printableH));
                pageElement.Arrange(new Rect(0, 0, printableW, printableH));
                pageElement.UpdateLayout();

                printDlg.PrintVisual(pageElement, jobTitle);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Printing error: {ex.Message}", "Print Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        return false;
    }

    public static void ExportToPdfOrXps(FrameworkElement pageElement, string destinationPath, bool isLandscape = false)
    {
        double w = isLandscape ? A4Height : A4Width;
        double h = isLandscape ? A4Width : A4Height;
        var rtb = RenderPageToBitmap(pageElement, w, h);

        // If path ends in .png, save PNG
        if (destinationPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            KidsColoringBook.DrawingEngine.CanvasExporter.SaveToPng(rtb, destinationPath);
            return;
        }

        // If path ends in .pdf, export genuine ISO 32000-1 / PDF 1.4 document
        if (destinationPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            PdfExporter.ExportBitmapToPdf(rtb, destinationPath, isLandscape);
            return;
        }

        // Otherwise, save as XPS document
        var fixedDoc = new FixedDocument();
        fixedDoc.DocumentPaginator.PageSize = new Size(w, h);

        var pageContent = new PageContent();
        var fixedPage = new FixedPage
        {
            Width = w,
            Height = h
        };

        var img = new Image
        {
            Source = rtb,
            Width = w,
            Height = h
        };
        fixedPage.Children.Add(img);
        ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
        fixedDoc.Pages.Add(pageContent);

        // Export via XPS package or if PDF requested, write XPS / PDF container
        string? dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Save as XPS / PDF document
        using (var package = System.IO.Packaging.Package.Open(destinationPath, FileMode.Create))
        {
            using (var xpsDoc = new System.Windows.Xps.Packaging.XpsDocument(package))
            {
                var xpsWriter = System.Windows.Xps.Packaging.XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                xpsWriter.Write(fixedDoc);
            }
        }
    }
}
