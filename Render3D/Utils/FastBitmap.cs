using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Utils
{
    unsafe class BmpPixelSnoop : IDisposable
    {
        private readonly Bitmap wrappedBitmap;
        private BitmapData data = null;
        private readonly byte* scan0;
        private readonly int depth;
        private readonly int stride;
        private readonly int width;
        private readonly int height;

        public BmpPixelSnoop(Bitmap bitmap)
        {
            wrappedBitmap = bitmap ?? throw new ArgumentException("Bitmap parameter cannot be null", "bitmap");
            width = wrappedBitmap.Width;
            height = wrappedBitmap.Height;

            var rect = new Rectangle(0, 0, wrappedBitmap.Width, wrappedBitmap.Height);

            try
            {
                data = wrappedBitmap.LockBits(rect, ImageLockMode.ReadOnly, wrappedBitmap.PixelFormat);
            }
            catch (Exception ex)
            {
                throw new System.InvalidOperationException("Could not lock bitmap, is it already being snooped somewhere else?", ex);
            }

            depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;
            scan0 = (byte*)data.Scan0.ToPointer();
            stride = data.Stride;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (wrappedBitmap != null)
                    wrappedBitmap.UnlockBits(data);
            }
        }

        private byte* PixelPointer(int x, int y)
        {
            return scan0 + y * stride + x * depth;
        }

        public System.Drawing.Color GetPixel(int x, int y)
        {
            // Better do the 'decent thing' and bounds check x & y
            if (x < 0 || y < 0 || x >= width || y >= height)
                throw new ArgumentException("x or y coordinate is out of range");

            int r, g, b;
            byte* p = PixelPointer(x, y);
            b = *p++;
            g = *p++;
            r = *p;

            return System.Drawing.Color.FromArgb(0, r, g, b);
        }


        public int Width { get { return width; } }

        // The bitmap's height
        public int Height { get { return height; } }
    }
}
