using static Globals.IComparableMethods;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace UserAvatars
{
    public static class ImageComparer
    {
        const int DefaultScalingFactor = 16;
        const int DefaultSegmentThreshold = 3;
        const float DefaultImageThreshold = 0.844f;

        public static bool Compare<TColorA, TColorB>(
            Image<TColorA> expected,
            Image<TColorB> actual,
            float imageTheshold = DefaultImageThreshold,
            byte segmentThreshold = DefaultSegmentThreshold,
            int scalingFactor = DefaultScalingFactor)
            where TColorA : unmanaged, IPixel<TColorA>
            where TColorB : unmanaged, IPixel<TColorB>
        {
            var percentage = expected.PercentageDifference(actual, segmentThreshold, scalingFactor);

            return percentage.InRange(0, imageTheshold);
        }

        public static float PercentageDifference<TColorA, TColorB>(
            this Image<TColorA> source,
            Image<TColorB> target,
            byte segmentThreshold = DefaultSegmentThreshold,
            int scalingFactor = DefaultScalingFactor)
            where TColorA : unmanaged, IPixel<TColorA>
            where TColorB : unmanaged, IPixel<TColorB>
        {
            // Code adapted from https://www.codeproject.com/Articles/374386/Simple-image-comparison-in-NET
            var differences = GetDifferences(source, target, scalingFactor);
            var diffPixels = 0;

            foreach (var b in differences.Data)
            {
                if (b > segmentThreshold)
                {
                    diffPixels++;
                }
            }

            return diffPixels / 256f;
        }

        private static DenseMatrix<byte> GetDifferences<TColorA, TColorB>(
            Image<TColorA> source,
            Image<TColorB> target,
            int scalingFactor)
            where TColorA : unmanaged, IPixel<TColorA>
            where TColorB : unmanaged, IPixel<TColorB>
        {
            var differences = new DenseMatrix<byte>(scalingFactor, scalingFactor);
            var firstGray = source.GetGrayScaleValues(scalingFactor);
            var secondGray = target.GetGrayScaleValues(scalingFactor);

            for (int y = 0; y < scalingFactor; y++)
            {
                for (int x = 0; x < scalingFactor; x++)
                {
                    differences[x, y] = (byte)Math.Abs(firstGray[x, y] - secondGray[x, y]);
                }
            }

            return differences;
        }

        private static DenseMatrix<byte> GetGrayScaleValues<TColorA>(
            this Image<TColorA> source,
            int scalingFactor)
            where TColorA : unmanaged, IPixel<TColorA>
        {
            var pixel = Rgba32.ParseHex("#000000");

            var clonedImage = source.Clone();
            clonedImage.Mutate(context => context.Resize(scalingFactor, scalingFactor));
            clonedImage.Mutate(context => context.Grayscale());

            using (clonedImage)
            {
                var grayScale = new DenseMatrix<byte>(scalingFactor, scalingFactor);

                for (int y = 0; y < scalingFactor; y++)
                {
                    for (int x = 0; x < scalingFactor; x++)
                    {
                        clonedImage[x, y].ToRgba32(ref pixel);
                        grayScale[x, y] = pixel.R;
                    }
                }

                return grayScale;
            }
        }
    }
}
