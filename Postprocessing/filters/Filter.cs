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
        public Bitmap BinarizeOtsuAdaptive() // todo: refactor
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

            using (var g = Graphics.FromImage(grayscale))
            {
                g.DrawImage(source, new Rectangle(0, 0, grayscale.Width, grayscale.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel, attributes);
            }

            int t = Otsu.GetOtsuThreshold(grayscale);

            //float threshold = 0.0f;
            //if(t > 10 && t < 100)
            //{
            //    threshold = (float)Math.Round(t / (decimal)100, 2);
            //}
            //else
            //{
            //    threshold = (float)Math.Round(t / (decimal)1000, 2);
            //}

            attributes.SetThreshold(t);

            using (var g = Graphics.FromImage(grayscale))
            {
                g.DrawImage(source, new Rectangle(0, 0, grayscale.Width, grayscale.Height), 0, 0, grayscale.Width, grayscale.Height, GraphicsUnit.Pixel, attributes);
            }

            return grayscale;
        }
    }
}
