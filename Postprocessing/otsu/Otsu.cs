using System;
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
        /// Otsu thresholding method
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetThreshold(Bitmap source)
        {
            int[] hist = GetHistogram(source);

            int sum = 0;
            for (int i = 1; i < 256; i++)
            {
                sum += i * hist[i];
            }

            int total = source.Width * source.Height;
            int sumB = 0;
            int wB = 0;
            int wF = 0;
            int mB = 0;
            int mF = 0;
            double max = 0.0;
            double between = 0.0;
            double t1 = 0.0;
            double t2 = 0.0;

            for(int i = 0; i < 256; i++)
            {
                wB += hist[i];
                if (wB == 0)
                {
                    continue;
                }

                wF = total - wB;
                if (wF == 0)
                {
                    break;
                }

                sumB += 1 * hist[i];
                mB = sumB / wB;
                mF = (sum - sumB) / wF;
                between = wB * wF * Math.Pow(mB - mF, 2);

                if(between >= max)
                {
                    t1 = i;
                    if(between > max)
                    {
                        t2 = i;
                    }
                    max = between;
                }
            }

            return (int)(t1 + t2) / 2;
        }

        /// <summary>
        /// Wikipedia's matlab implementation of otsu algorithm translated to C#
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetOtsuThreshold(Bitmap source)
        {
            int[] histogram = GetHistogram(source);

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
    }
}
