﻿using System;
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
        private static int[] GetHistogram(Bitmap source) // todo: refactor, needs to be faster
        {
            using(Bitmap clone = (Bitmap)source.Clone())
            {
                int[] histogram = new int[256];

                BitmapData data = clone.LockBits(new Rectangle(Point.Empty, source.Size), ImageLockMode.ReadOnly, source.PixelFormat);

                for(int x = 0; x < source.Width; x++)
                {
                    for(int y = 0; y < source.Height; y++)
                    {
                        //int value = (source.GetPixel(x, y).R + source.GetPixel(x, y).G + source.GetPixel(x, y).B) / 3;
                        int value = source.GetPixel(x, y).R;

                        histogram[value] += 1;
                    }
                }

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