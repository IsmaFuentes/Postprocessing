using Postprocessing.otsu;
using System;
using System.Drawing;
using System.Drawing.Imaging;

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
            if(this.source != null)
            {
                source.Dispose();
            }
        }

        /// <summary>
        /// Applies a grayscale filter
        /// </summary>
        /// <returns></returns>
        public Bitmap ToGrayscale()
        {
            var grayscale = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var attributes = new ImageAttributes();

            // Grayscale color matrix
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

        public Bitmap Binarize(float strength)
        {
            if(strength < 0 || strength > 1)
            {
                throw new ArgumentOutOfRangeException("strength should be between 0 and 1");
            }

            var grayscale = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var attributes = new ImageAttributes();

            // Grayscale color matrix
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
        /// Applies a black and white filter using the commonly known Otsu adaptive thresholding algorithm
        /// </summary>
        /// <returns></returns>
        public Bitmap BinarizeOtsuAdaptive()
        {
            var binarized = new Bitmap(source.Width, source.Height, source.PixelFormat);

            using(var grayscale = this.ToGrayscale())
            {
                var attributes = new ImageAttributes();

                int t = Otsu.GetOtsuThreshold(grayscale);

                attributes.SetThreshold(t);

                using(var g = Graphics.FromImage(binarized))
                {
                    g.DrawImage(grayscale, new Rectangle(0, 0, grayscale.Width, grayscale.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            return binarized;
        }

        /// <summary>
        /// Applies a sharpen filter into the image
        /// </summary>
        /// <param name="strength"></param>
        /// <returns></returns>
        public Bitmap Sharpen(double strength)
        {
            if (strength < 0 || strength > 1)
            {
                throw new ArgumentOutOfRangeException("strength should be between 0 and 1");
            }

            // Todo: make it faster

            var sharpen = (Bitmap)source.Clone();

            const int fW = 5; // filter width
            const int fH = 5; // fitler height

            int w = sharpen.Width;
            int h = sharpen.Height;

            double[,] filter = new double[fW, fH]
            {
                { -1, -1, -1, -1, -1 },
                { -1,  2,  2,  2, -1 },
                { -1,  2,  16,  2, -1 },
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

                    for (int fx = 0; fx < fW; fx++)
                    {
                        for (int fy = 0; fy < fH; fy++)
                        {
                            int imageX = (x - fW / 2 + fx + w) % w;
                            int imageY = (y - fH / 2 + fy + h) % h;

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

            for (int x = 0; x < w; ++x)
            {
                for (int y = 0; y < h; ++y)
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

