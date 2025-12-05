using ScreenshotMcp.Server.Models;
using SkiaSharp;

namespace ScreenshotMcp.Server.Services;

public interface IImageProcessor
{
    /// <summary>
    /// Processes raw screenshot bytes according to the image options.
    /// Handles format conversion, quality adjustment, and scaling.
    /// </summary>
    (byte[] Data, string MimeType) Process(byte[] pngBytes, ImageOptions options);
}

public class ImageProcessor : IImageProcessor
{
    public (byte[] Data, string MimeType) Process(byte[] pngBytes, ImageOptions options)
    {
        var normalizedOptions = options.Normalize();

        // If no processing needed (PNG at full scale), return original
        if (normalizedOptions.Format == "png" && normalizedOptions.Scale >= 1.0f)
        {
            return (pngBytes, "image/png");
        }

        using var originalBitmap = SKBitmap.Decode(pngBytes);
        if (originalBitmap == null)
        {
            // Failed to decode, return original
            return (pngBytes, "image/png");
        }

        SKBitmap bitmapToEncode = originalBitmap;
        SKBitmap? resizedBitmap = null;

        try
        {
            // Scale if needed
            if (normalizedOptions.Scale < 1.0f)
            {
                var newWidth = (int)(originalBitmap.Width * normalizedOptions.Scale);
                var newHeight = (int)(originalBitmap.Height * normalizedOptions.Scale);

                // Ensure minimum dimensions
                newWidth = Math.Max(1, newWidth);
                newHeight = Math.Max(1, newHeight);

                resizedBitmap = new SKBitmap(newWidth, newHeight);
                using var canvas = new SKCanvas(resizedBitmap);
                using var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.Medium,
                    IsAntialias = true
                };

                canvas.DrawBitmap(originalBitmap,
                    new SKRect(0, 0, newWidth, newHeight),
                    paint);

                bitmapToEncode = resizedBitmap;
            }

            // Encode to target format
            using var image = SKImage.FromBitmap(bitmapToEncode);

            if (normalizedOptions.Format == "jpeg")
            {
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, normalizedOptions.Quality);
                return (data.ToArray(), "image/jpeg");
            }
            else
            {
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                return (data.ToArray(), "image/png");
            }
        }
        finally
        {
            resizedBitmap?.Dispose();
        }
    }
}
