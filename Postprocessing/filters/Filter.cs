using System;
using System.Drawing;
using System.Drawing.Imaging;
using Postprocessing.otsu;

namespace Postprocessing.filters
{
    public class Filter : IDisposable
    {
        private Bitmap source;

        public Filter(Bitmap source)
        {
            this.source = (Bitmap)source.Clone();
        }

        public void Dispose()
        {
            if(source != null)
            {
                source.Dispose();
            }
        }

        /// <summary>
        /// Returns a grayscale filtered image
        /// </summary>
        /// <returns></returns>
        public Bitmap ToGrayscale()
        {
            var grayscale = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var attributes = new ImageAttributes();

            ColorMatrix grayscaleMatrix = new ColorMatrix(new float[][]
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {     0,      0,      0, 1, 0},
                new float[] {     0,      0,      0, 0, 1}
            });

            attributes.SetColorMatrix(grayscaleMatrix);

            using(var g = Graphics.FromImage(grayscale))
            {
                g.DrawImage(source, new Rectangle(0, 0, grayscale.Width, grayscale.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel, attributes);
            }

            return grayscale;
        }

        /// <summary>
        /// Returns a binarized image
        /// </summary>
        /// <param name="strength"></param>
        /// <returns></returns>
        public Bitmap Binarize(float strength)
        {
            if(strength < 0 || strength > 1)
            {
                throw new ArgumentOutOfRangeException("strength should be between 0 and 1");
            }

            var grayscale = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var attributes = new ImageAttributes();

            ColorMatrix grayscaleMatrix = new ColorMatrix(new float[][]
            {
                new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                new float[] {     0,      0,      0, 1, 0},
                new float[] {     0,      0,      0, 0, 1}
            });

            attributes.SetColorMatrix(grayscaleMatrix);
            attributes.SetThreshold(strength);

            using (var g = Graphics.FromImage(grayscale))
            {
                g.DrawImage(source, new Rectangle(0, 0, grayscale.Width, grayscale.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel, attributes);
            }

            return grayscale;
        }

        /// <summary>
        /// Returns a binarized image using otsu's adaptative thresholding algorithm
        /// </summary>
        /// <returns></returns>
        public Bitmap BinarizeOtsuAdaptive()
        {
            using(var grayscale = ToGrayscale())
            {
                Bitmap binarized = (Bitmap)grayscale.Clone();
                var data = binarized.LockBits(new Rectangle(0, 0, grayscale.Width, grayscale.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int threshold = Otsu.GetThreshold(grayscale);
                unsafe
                {
                    int totalRGB;
                    byte* ptr = (byte*)data.Scan0.ToPointer();
                    int stopAddress = (int)ptr + data.Stride * data.Height;
                    while((int)ptr != stopAddress)
                    {
                        totalRGB = ptr[0] + ptr[1] + ptr[2];
                        if(totalRGB <= threshold)
                        {
                            ptr[2] = 0;
                            ptr[1] = 0;
                            ptr[0] = 0;
                        }
                        else
                        {
                            ptr[2] = 255;
                            ptr[1] = 255;
                            ptr[0] = 255;
                        }

                        ptr += 3;
                    }
                }

                binarized.UnlockBits(data);

                return binarized;
            }
        }

        /// <summary>
        /// Returns a sharpened image
        /// </summary>
        /// <param name="strength"></param>
        /// <returns></returns>
        public Bitmap Sharpen(double strength)
        {
            if (strength < 0 || strength > 1)
            {
                throw new ArgumentOutOfRangeException("strength should be between 0 and 1");
            }

            var sharpen = (Bitmap)source.Clone();

            const int filterSize = 5;//5;

            int w = sharpen.Width;
            int h = sharpen.Height;

            double[,] filter = new double[filterSize, filterSize]
            {
                {  0, -1, -1, -1,  0 },
                { -1,  2,  2,  2, -1 },
                { -1,  2,  16, 2, -1 },
                { -1,  2,  2,  2, -1 },
                {  0, -1, -1, -1,  0 }
            };

            double bias = 1 - strength;
            double factor = strength / 16.0;

            Color[,] result = new Color[w, h];
            var data = sharpen.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int bytes = data.Stride * h;
            byte[] rgbValues = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

            int rgb;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    double rColor = 0.0;
                    double gColor = 0.0;
                    double bColor = 0.0;

                    for (int fx = 0; fx < filterSize; fx++)
                    {
                        for (int fy = 0; fy < filterSize; fy++)
                        {
                            int imageX = (x - filterSize / 2 + fx + w) % w;
                            int imageY = (y - filterSize / 2 + fy + h) % h;

                            rgb = imageY * data.Stride + 3 * imageX;

                            rColor += rgbValues[rgb + 2] * filter[fx, fy];
                            gColor += rgbValues[rgb + 1] * filter[fx, fy];
                            bColor += rgbValues[rgb + 0] * filter[fx, fy];
                        }

                        int r = Math.Min(Math.Max((int)(factor * rColor + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * gColor + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * bColor + bias), 0), 255);

                        result[x, y] = Color.FromArgb(r, g, b);
                    }

                    rgb = y * data.Stride + 3 * x;

                    rgbValues[rgb + 2] = result[x, y].R;
                    rgbValues[rgb + 1] = result[x, y].G;
                    rgbValues[rgb + 0] = result[x, y].B;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, data.Scan0, bytes);

            sharpen.UnlockBits(data);

            return sharpen;
        }

        public Bitmap Sharpen_old(double strength)
        {
            if (strength < 0 || strength > 1)
            {
                throw new ArgumentOutOfRangeException("strength should be between 0 and 1");
            }

            var sharpen = (Bitmap)source.Clone();

            const int filterW = 5;
            const int filterH = 5;

            int w = sharpen.Width;
            int h = sharpen.Height;

            double[,] filter = new double[filterW, filterH]
            {
                { -1, -1, -1, -1, -1 },
                { -1,  2,  2,  2, -1 },
                { -1,  2,  16, 2, -1 },
                { -1,  2,  2,  2, -1 },
                { -1, -1, -1, -1, -1 }
            };

            double bias = 1 - strength;
            double factor = strength / 16.0;

            Color[,] result = new Color[w, h];
            var data = sharpen.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int bytes = data.Stride * h;
            byte[] rgbValues = new byte[bytes];

            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

            int rgb;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    double rColor = 0.0;
                    double gColor = 0.0;
                    double bColor = 0.0;

                    for (int fx = 0; fx < filterW; fx++)
                    {
                        for (int fy = 0; fy < filterH; fy++)
                        {
                            int imageX = (x - filterW / 2 + fx + w) % w;
                            int imageY = (y - filterH / 2 + fy + h) % h;

                            rgb = imageY * data.Stride + 3 * imageX;

                            rColor += rgbValues[rgb + 2] * filter[fx, fy];
                            gColor += rgbValues[rgb + 1] * filter[fx, fy];
                            bColor += rgbValues[rgb + 0] * filter[fx, fy];
                        }

                        int r = Math.Min(Math.Max((int)(factor * rColor + bias), 0), 255);
                        int g = Math.Min(Math.Max((int)(factor * gColor + bias), 0), 255);
                        int b = Math.Min(Math.Max((int)(factor * bColor + bias), 0), 255);

                        result[x, y] = Color.FromArgb(r, g, b);
                    }
                }
            }

            // update image rgb values
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    rgb = y * data.Stride + 3 * x;

                    rgbValues[rgb + 2] = result[x, y].R;
                    rgbValues[rgb + 1] = result[x, y].G;
                    rgbValues[rgb + 0] = result[x, y].B;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, data.Scan0, bytes);

            sharpen.UnlockBits(data);

            return sharpen;
        }
    }
}

