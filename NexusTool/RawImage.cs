
using ImageMagick;

namespace NexusTool;

public class RawImage
{
    public static List<byte[]> LoadRawImages(string name, int frame = 0)
    {
        using MagickImageCollection collection = new(name);
        collection.Coalesce();

        if (frame < 0 || frame > collection.Count)
            throw new ArgumentOutOfRangeException(nameof(frame), $"Out of range: frame=0 for all, or 1-{collection.Count}");

        List<byte[]> rawImages = [];
        int currentFrame = 0;

        foreach (MagickImage image in collection)
        {
            currentFrame++;
            if (frame != 0 && currentFrame != frame) continue;

            if (!Program.Quiet)
            {
                Console.WriteLine($"Frame : {currentFrame}");
                Console.WriteLine($"Width : {image.Width}");
                Console.WriteLine($"Height: {image.Height}");
                Console.WriteLine($"Format: {image.Format}");
            }

            MagickGeometry destSize = new(640, 48);
            destSize.IgnoreAspectRatio = true;

            image.Resize(destSize);

            IPixelCollection<ushort> pixels = image.GetPixels();

            byte[] rawImage = new byte[640 * 48 * 4];  // 640x48xRGBA(byte)
            rawImages.Add(rawImage);

            int i = 0;
            for (int y = 0; y < 48; ++y)
                for (int x = 0; x < 640; ++x)
                {
                    IMagickColor<ushort> pixel = pixels.GetPixel(x, y).ToColor() ?? throw new InvalidDataException();
                    byte[] pxbytes = pixel.ToByteArray();

                    rawImage[i] = pxbytes[0];       // R
                    rawImage[i + 1] = pxbytes[1];   // G
                    rawImage[i + 2] = pxbytes[2];   // B
                    rawImage[i + 3] = 255;          // Set A to opaque

                    i += 4;
                }

        }

        return rawImages;
    }
}