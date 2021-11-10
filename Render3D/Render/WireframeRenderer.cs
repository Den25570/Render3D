using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Render3D.Render
{
    public class WireframeRenderer : IRenderer
    {
        private WriteableBitmap _bitmap;
        float[] _zBuffer;
        private int _width;
        private int _height;

        public bool HasBitmap { get => _bitmap != null; }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _zBuffer = new float[height * width];
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);

            var image = new System.Windows.Controls.Image();
            image.Source = _bitmap;
            canvas.Children.Clear();
            canvas.Children.Add(image);
        }

        public void RenderModel(Model model, Material material, Scene world)
        {
            try
            {
                _bitmap.Lock();
                Array.Fill(_zBuffer, float.MaxValue);
                _bitmap.Clear(System.Windows.Media.Color.FromRgb(0, 0, 0));

                foreach (var triangle in model.Triangles)
                {
                    DrawLine((int)triangle.Points[0].X, (int)triangle.Points[0].Y, (int)triangle.Points[1].X, (int)triangle.Points[1].Y, 0x00FF00);
                    DrawLine((int)triangle.Points[1].X, (int)triangle.Points[1].Y, (int)triangle.Points[2].X, (int)triangle.Points[2].Y, 0x00FF00);
                    DrawLine((int)triangle.Points[2].X, (int)triangle.Points[2].Y, (int)triangle.Points[0].X, (int)triangle.Points[0].Y, 0x00FF00);
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }
        }

        private void DrawLine(int x, int y, int x1, int y1, int color = 0x000000)
        {
            int w = x1 - x;
            int h = y1 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = System.Math.Abs(w);
            int shortest = System.Math.Abs(h);
            if (!(longest > shortest))
            {
                longest = System.Math.Abs(h);
                shortest = System.Math.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++)
            {
                if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight) DrawPixel(x, y, color);
                numerator += shortest;
                if (!(numerator < longest))
                {
                    numerator -= longest;
                    x += dx1;
                    y += dy1;
                }
                else
                {
                    x += dx2;
                    y += dy2;
                }
            }
        }

        private void DrawPixel(int x, int y, int color = 0x00000000)
        {
            unsafe
            {
                IntPtr pBackBuffer = _bitmap.BackBuffer;
                pBackBuffer += y * _bitmap.BackBufferStride;
                pBackBuffer += x * 4;
                *((int*)pBackBuffer) = color;
            }
        }
    }
}
