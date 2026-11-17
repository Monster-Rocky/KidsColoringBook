using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace KidsColoringBook.Printing;

/// <summary>
/// 100% offline, zero-dependency ISO 32000-1 (PDF 1.4) document generator.
/// Exports high-resolution A4 portrait and landscape documents with DCTDecode (JPEG) streams.
/// Universally compatible with Adobe Acrobat, Chrome, Edge, Safari, Firefox, and macOS Preview.
/// </summary>
public static class PdfExporter
{
    // A4 dimensions in PDF points (72 points per inch)
    public const double A4WidthPoints = 595.28;
    public const double A4HeightPoints = 841.89;

    public static void ExportBitmapToPdf(BitmapSource bitmap, string destinationPath, bool isLandscape = false, string title = "Kids Coloring Book")
    {
        if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

        string? dir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 1. Encode high-resolution bitmap to JPEG stream
        byte[] jpegBytes;
        using (var ms = new MemoryStream())
        {
            var encoder = new JpegBitmapEncoder { QualityLevel = 94 };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(ms);
            jpegBytes = ms.ToArray();
        }

        int imgPixelWidth = bitmap.PixelWidth;
        int imgPixelHeight = bitmap.PixelHeight;

        double pageW = isLandscape ? A4HeightPoints : A4WidthPoints;
        double pageH = isLandscape ? A4WidthPoints : A4HeightPoints;

        // 2. Assemble standard PDF 1.4 file
        using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var bw = new BinaryWriter(fs))
        {
            var offsets = new List<long>();
            Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");

            void WriteAscii(string str) => bw.Write(latin1.GetBytes(str));

            // Header: %PDF-1.4 followed by binary comment
            WriteAscii("%PDF-1.4\r\n%\xE2\xE3\xCF\xD3\r\n");

            // Object 1: Catalog
            offsets.Add(fs.Position);
            WriteAscii("1 0 obj\r\n<< /Type /Catalog /Pages 2 0 R >>\r\nendobj\r\n");

            // Object 2: Pages tree
            offsets.Add(fs.Position);
            WriteAscii("2 0 obj\r\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\r\nendobj\r\n");

            // Object 3: Page object
            offsets.Add(fs.Position);
            string pageObj = string.Format(CultureInfo.InvariantCulture,
                "3 0 obj\r\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {0:0.##} {1:0.##}] /Resources << /ProcSet [/PDF /ImageC] /XObject << /Im1 4 0 R >> >> /Contents 5 0 R >>\r\nendobj\r\n",
                pageW, pageH);
            WriteAscii(pageObj);

            // Object 4: Image XObject (JPEG DCTDecode)
            offsets.Add(fs.Position);
            string imgObj = string.Format(CultureInfo.InvariantCulture,
                "4 0 obj\r\n<< /Type /XObject /Subtype /Image /Width {0} /Height {1} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {2} >>\r\nstream\r\n",
                imgPixelWidth, imgPixelHeight, jpegBytes.Length);
            WriteAscii(imgObj);
            bw.Write(jpegBytes);
            WriteAscii("\r\nendstream\r\nendobj\r\n");

            // Object 5: Contents stream (maps image to cover A4 bounds)
            string contentOps = string.Format(CultureInfo.InvariantCulture,
                "q\r\n{0:0.##} 0 0 {1:0.##} 0 0 cm\r\n/Im1 Do\r\nQ\r\n",
                pageW, pageH);
            byte[] contentBytes = latin1.GetBytes(contentOps);

            offsets.Add(fs.Position);
            string contentObj = string.Format(CultureInfo.InvariantCulture,
                "5 0 obj\r\n<< /Length {0} >>\r\nstream\r\n{1}endstream\r\nendobj\r\n",
                contentBytes.Length, contentOps);
            WriteAscii(contentObj);

            // Object 6: Info Metadata
            offsets.Add(fs.Position);
            string creationDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            string safeTitle = title.Replace("(", "").Replace(")", "").Replace("\\", "");
            string infoObj = string.Format(CultureInfo.InvariantCulture,
                "6 0 obj\r\n<< /Title ({0}) /Creator (Kids Coloring Book Desktop App) /CreationDate (D:{1}) >>\r\nendobj\r\n",
                safeTitle, creationDate);
            WriteAscii(infoObj);

            // Cross-Reference Table (xref)
            long startXref = fs.Position;
            int totalObjs = offsets.Count + 1; // Object 0 + objects 1..6
            WriteAscii(string.Format(CultureInfo.InvariantCulture, "xref\r\n0 {0}\r\n", totalObjs));
            WriteAscii("0000000000 65535 f \r\n"); // Object 0 (20 bytes)
            for (int i = 0; i < offsets.Count; i++)
            {
                // Each xref entry is strictly 20 bytes: 10 digits + space + 5 digits + space + n + CRLF
                WriteAscii(string.Format(CultureInfo.InvariantCulture, "{0:D10} 00000 n \r\n", offsets[i]));
            }

            // Trailer
            string trailer = string.Format(CultureInfo.InvariantCulture,
                "trailer\r\n<< /Size {0} /Root 1 0 R /Info 6 0 R >>\r\nstartxref\r\n{1}\r\n%%EOF\r\n",
                totalObjs, startXref);
            WriteAscii(trailer);
        }
    }
}
