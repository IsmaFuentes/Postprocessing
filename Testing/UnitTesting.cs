using System;
using System.Drawing;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Postprocessing.filters;

namespace Testing
{
    [TestClass]
    public class UnitTesting
    {
        [TestMethod]
        public void TestGrayscale()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Directory.GetParent(dir).Parent.Parent.Parent.FullName;

            using (var source = Bitmap.FromFile($"{projectDirectory}/samples/portrait.jpg"))
            {
                using (var filter = new Filter((Bitmap)source))
                {
                    using (var grayscale = filter.ToGrayscale())
                    {
                        grayscale.Save(@"C:\Users\Ismael\Desktop\grayscale.jpg");
                    }
                }
            }
        }

        [TestMethod]
        public void TestBinarize()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Directory.GetParent(dir).Parent.Parent.Parent.FullName;

            using (var source = Bitmap.FromFile($"{projectDirectory}/samples/portrait.jpg"))
            {
                using (var filter = new Filter((Bitmap)source))
                {
                    using (var grayscale = filter.Binarize(0.40f))
                    {
                        grayscale.Save(@"C:\Users\Ismael\Desktop\binarized.jpg");
                    }
                }
            }
        }

        [TestMethod]
        public void TestOtsuThresholding()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Directory.GetParent(dir).Parent.Parent.Parent.FullName;

            using (var source = Bitmap.FromFile($"{projectDirectory}/samples/portrait2.jpg"))
            {
                using (var filter = new Filter((Bitmap)source))
                {
                    using(var binarized = filter.BinarizeOtsuAdaptive())
                    {
                        binarized.Save(@"C:\Users\Ismael\Desktop\binarized_otsu.jpg");
                    }
                }
            }
        }

        [TestMethod]
        public void TestSharpen()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDirectory = Directory.GetParent(dir).Parent.Parent.Parent.FullName;

            using (var source = Bitmap.FromFile($"{projectDirectory}/samples/portrait.jpg"))
            {
                using (var filter = new Filter((Bitmap)source))
                {
                    using (var sharpened = filter.Sharpen(0.85))
                    {
                        sharpened.Save(@"C:\Users\Ismael\Desktop\sharpened.jpg");
                    }
                }
            }
        }
    }
}
