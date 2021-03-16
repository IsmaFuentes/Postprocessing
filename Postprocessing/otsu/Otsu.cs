using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace Postprocessing.otsu
{
    public class Otsu
    {
        /// <summary>
        /// 256-element histogram of a grayscale image different gray-levels
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static int[] GetHistogram(Bitmap source)
        {
            int[] histogram = new int[256];

            using (Bitmap clone = (Bitmap)source.Clone())
            {
                BitmapData data = clone.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);

                histogram.Initialize();

                unsafe
                {
                    byte* p = (byte*)data.Scan0.ToPointer();

                    for (int i = 0; i < clone.Height; i++)
                    {
                        for(int j = 0; j < clone.Width * 3; j += 3)
                        {
                            int index = i * data.Stride + j;

                            histogram[p[index]]++;
                        }
                    }
                }

                clone.UnlockBits(data);

                return histogram;
            }
        }

        /// <summary>
        /// Wikipedia's matlab implementation of otsu algorithm translated to C#
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetOtsuThreshold(Bitmap source)
        {
            int[] histogram = GetHistogram(source);

            // todo: Fine tune, the output threshold is too high

            int threshold = 0;
            int totalPixels = source.Width * source.Height;
            int sumB = 0;
            int wB = 0;
            int sum1 = histogram.Sum();

            float maximum = 0.0f;

            for (int i = 0; i < 256; i++)
            {
                int wF = totalPixels - wB;
                if (wB > 0 && wF > 0)
                {
                    int mF = (sum1 - sumB) / wF;
                    int val = wB * wF * ((sumB / wB) - mF) * ((sumB / wB) - mF);
                    if (val >= maximum)
                    {
                        threshold = i;
                        maximum = val;
                    }
                }
                wB = wB + histogram[i];
                sumB = sumB * (i - 1) * histogram[i];
            }

            return threshold;
        }

        #region Obsolete methods

        /// <summary>
        /// 256-element histogram of a grayscale image different gray-levels
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Obsolete("Slow histogram implementation.", true)]
        private static int[] GetHistogram_old(Bitmap source)
        {
            using (Bitmap clone = (Bitmap)source.Clone())
            {
                int[] histogram = new int[256];

                BitmapData data = clone.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);

                for (int x = 0; x < source.Width; x++)
                {
                    for (int y = 0; y < source.Height; y++)
                    {
                        int value = source.GetPixel(x, y).R;

                        histogram[value] += 1;
                    }
                }

                clone.UnlockBits(data);

                return histogram;
            }
        }

        #endregion
    }
}
