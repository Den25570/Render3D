using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
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
    public class TriangleRenderer : IRenderer
    {
        private WriteableBitmap _bitmap;
        float[,] _zBuffer;
        private int _width;
        private int _height;

        public bool HasBitmap { get => _bitmap != null; }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _zBuffer = new float[width, height];
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            var image = new System.Windows.Controls.Image();
            image.Source = _bitmap;
            canvas.Children.Clear();
            canvas.Children.Add(image);
        }

        public void RenderModel(Model model, Vector3 lightDirection)
        {
            try
            {
                _bitmap.Lock();
                Array.Clear(_zBuffer, 0, _zBuffer.Length);
                _bitmap.Clear(System.Windows.Media.Color.FromRgb(0, 0, 0));
                //Draw wireframe
                foreach (var triangle in model.Triangles)
                {
                    var MinX = (int)MathF.Min(MathF.Min(triangle.Points[0].X, triangle.Points[1].X), triangle.Points[2].X) - 1;
                    var MinY = (int)MathF.Min(MathF.Min(triangle.Points[0].Y, triangle.Points[1].Y), triangle.Points[2].Y) - 1;
                    var MaxX = (int)MathF.Max(MathF.Max(triangle.Points[0].X, triangle.Points[1].X), triangle.Points[2].X) + 1;
                    var MaxY = (int)MathF.Max(MathF.Max(triangle.Points[0].Y, triangle.Points[1].Y), triangle.Points[2].Y) + 1;

                    //Draw edges
                    //DrawLine((int)triangle.Points[0].X, (int)triangle.Points[0].Y, (int)triangle.Points[1].X, (int)triangle.Points[1].Y, 0x00FF00);
                    //DrawLine((int)triangle.Points[1].X, (int)triangle.Points[1].Y, (int)triangle.Points[2].X, (int)triangle.Points[2].Y, 0x00FF00);
                    //DrawLine((int)triangle.Points[2].X, (int)triangle.Points[2].Y, (int)triangle.Points[0].X, (int)triangle.Points[0].Y, 0x00FF00);

                    //Draw triangle
                    var dt = MathF.Max((Vector3.Dot(triangle.Normal, lightDirection) + 1) / 2, 0.25f);
                    var color = (int)(0xFF * dt) * 0x100 * 0x100 + (int)(0xFF * dt) * 0x100 + (int)(0xFF * dt);
                    var z = (triangle.Points[0].Z + triangle.Points[1].Z + triangle.Points[2].Z) / 3;

                    DrawTriangle(
                        (int)triangle.Points[0].X, (int)triangle.Points[0].Y,
                        (int)triangle.Points[1].X, (int)triangle.Points[1].Y,
                        (int)triangle.Points[2].X, (int)triangle.Points[2].Y,
                        z, color);

                    /*for (int x = MinX; x < MaxX; x++)
                    {
                        for (int y = MinY; y < MaxY; y++)
                        {
                            var ef1 = EdgeFunction(triangle.Points[0].X, triangle.Points[0].Y, triangle.Points[1].X, triangle.Points[1].Y, x, y);
                            var ef2 = EdgeFunction(triangle.Points[1].X, triangle.Points[1].Y, triangle.Points[2].X, triangle.Points[2].Y, x, y);
                            var ef3 = EdgeFunction(triangle.Points[2].X, triangle.Points[2].Y, triangle.Points[0].X, triangle.Points[0].Y, x, y);
                            if ((ef1 >= 0 && ef2 >= 0 && ef3 >= 0) || (ef1 <= 0 && ef2 <= 0 && ef3 <= 0))
                            {
                                if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                                {
                                    if (z < _zBuffer[x,y] || _zBuffer[x, y] == 0)
                                    {
                                        DrawPixel(x, y, color);
                                        _zBuffer[x, y] = z;
                                    }
                                }            
                            }
                        }
                    }*/
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private float EdgeFunction(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            return (x1 - x2) * (y3 - y1) - (y1 - y2) * (x3 - x1);
        }

        private void swap(ref int a, ref int b)
        {
            int t = a;
            a = b;
            b = t;
        }
        private void swap(ref float a, ref float b)
        {
            float t = a;
            a = b;
            b = t;
        }

        private void DrawTriangle(int x1, int y1, int x2, int y2, int x3, int y3, float z, int color)
        {
            if (y2 < y1)
            {
                swap(ref y1, ref y2);
                swap(ref x1, ref x2);
            }
            if (y3 < y1)
            {
                swap(ref y1, ref y3);
                swap(ref x1, ref x3);
            }
            if (y2 > y3)
            {
                swap(ref y2, ref y3);
                swap(ref x2, ref x3);
            }
            float dx13 = 0, dx12 = 0, dx23 = 0;
            if (y3 != y1)
            {
                dx13 = x3 - x1;
                dx13 /= y3 - y1;
            }
            if (y2 != y1)
            {
                dx12 = x2 - x1;
                dx12 /= (y2 - y1);
            }
            if (y3 != y2)
            {
                dx23 = x3 - x2;
                dx23 /= (y3 - y2);
            }
            float wx1 = x1;
            float wx2 = wx1;
            float _dx13 = dx13;
            if (dx13 > dx12)
            {
                swap(ref dx13, ref dx12);
            }
            // растеризуем верхний полутреугольник
            for (int y = (int)y1; y < (int)y2; y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                for (int x = (int)wx1; x <= (int)wx2; x++)
                {
                    if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        if (z < _zBuffer[x, y] || _zBuffer[x, y] == 0)
                        {
                            DrawPixel(x, y, color);
                            _zBuffer[x, y] = z;
                        }
                    }
                }
                wx1 += dx13;
                wx2 += dx12;
            }
            // вырожденный случай, когда верхнего полутреугольника нет
            // надо разнести рабочие точки по оси x, т.к. изначально они совпадают
            if (y1 == y2)
            {
                wx1 = x1 < x2 ? x1 : x2;
                wx2 = x1 >= x2 ? x1 : x2;
            }
            // упорядочиваем приращения
            // (используем сохраненное приращение)
            if (_dx13 < dx23)
            {
                swap(ref _dx13, ref dx23);
            }
            // растеризуем нижний полутреугольник
            for (int y = (int)y2; y <= (int)y3; y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                for (int x = (int)wx1; x <= (int)wx2; x++)
                {
                    if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        if (z < _zBuffer[x, y] || _zBuffer[x, y] == 0)
                        {
                            DrawPixel(x, y, color);
                            _zBuffer[x, y] = z;
                        }
                    }
                }
                wx1 += _dx13;
                wx2 += dx23;
            }
        }

        private void DrawLine(int x, int y, int x2, int y2, float z, int color = 0x000000)
        {
            int w = x2 - x;
            int h = y2 - y;
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
