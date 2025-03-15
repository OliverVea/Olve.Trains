using System.Drawing;

namespace Olve.Engine3D.IO.Images;

public class GrayscaleImageReader
{
    public static byte[,] ReadGrayscaleImage(string imagePath)
    {
        // Load the image
        using (Bitmap bitmap = new Bitmap(imagePath))
        {
            // Ensure the image is 8-bit grayscale
            if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format8bppIndexed)
            {
                throw new InvalidOperationException("The image must be 8-bit grayscale.");
            }

            int width = bitmap.Width;
            int height = bitmap.Height;
            byte[,] pixelData = new byte[height, width];

            // Loop through each pixel in the image
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get the pixel color at (x, y)
                    Color color = bitmap.GetPixel(x, y);

                    // Since it's a grayscale image, we can directly use the R, G, or B value (they should all be the same)
                    pixelData[y, x] = color.R;  // R, G, B are the same for grayscale
                }
            }

            return pixelData;
        }
    }
}