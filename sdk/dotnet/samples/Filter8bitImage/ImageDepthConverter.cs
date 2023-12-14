using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace Filter8bitImage
{
    public static class ImageDepthConverter
    {
        public static bool Convert8BitGrayTo12BitGray(string imageFile8bit, string imageFile12bit)
        {
            // Load the input image as an 8-bit image file
            using (var imageIn = Image.Load<L8>(imageFile8bit))
            {
                var width = imageIn.Width;
                var height = imageIn.Height;

                // Clone the image as a 16-bit type so we have a new image to manipulate
                using (var imageOut = imageIn.CloneAs<L16>())
                {
                    // ImageSharp automatically scales the pixels from 8-bit to 16-bit, but
                    // we want to scale to 12-bit. So, we will overwrite the pixel data
                    // manually using the 8-bit data and shifting by 4 bits only
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            // Read the 8-bit pixel and shift by 4 bits (multiply by 16)
                            var pixel = imageIn[x, y];
                            imageOut[x, y] = new L16((ushort)(pixel.PackedValue << 4));
                        }
                    }

                    // Save the 12-bit image as a 16-bit PNG
                    imageOut.Save(imageFile12bit, new PngEncoder()
                    {
                        BitDepth = PngBitDepth.Bit16,
                        ColorType = PngColorType.Grayscale
                    });
                }
            }

            return true;
        }

        public static bool Convert16BitGrayTo8BitGray(Stream imageStream16bit, string imageFile8bit)
        {
            // Load the input image as an 16-bit image file
            using (var imageIn = Image.Load<L16>(imageStream16bit))
            {
                var width = imageIn.Width;
                var height = imageIn.Height;

                // Clone the image as a 8-bit type so we have a new image to manipulate
                using (var imageOut = imageIn.CloneAs<L8>())
                {
                    // Scale pixel data by shifting 8 bits
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            // Read the 16-bit pixel and shift by 8 bits (divide by 256)
                            var pixel = imageIn[x, y];
                            imageOut[x, y] = new L8((byte)(pixel.PackedValue / 256));
                        }
                    }

                    // Save the 8-bit image as a 8-bit PNG
                    imageOut.Save(imageFile8bit, new PngEncoder()
                    {
                        BitDepth = PngBitDepth.Bit8,
                        ColorType = PngColorType.Grayscale
                    });
                }
            }

            return true;
        }

        public static (UInt16,UInt16) GetMinMaxGrayScale(string filename)
        {
            UInt16 min = UInt16.MaxValue;
            UInt16 max = 0;
            using (var image = Image.Load<L16>(filename))
            {
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var pixelRow = accessor.GetRowSpan(y);

                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            min = Math.Min(min, pixelRow[x].PackedValue);
                            max = Math.Max(max, pixelRow[x].PackedValue);
                        }
                    }
                });

                return (min, max);
            }
        }
    }
}
