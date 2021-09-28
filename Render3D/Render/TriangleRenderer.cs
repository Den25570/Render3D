using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
using Render3D.Utils;
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

        public void RenderModel(Model model, World world)
        {
            try
            {
                _bitmap.Lock();
                Array.Fill(_zBuffer, float.MaxValue);
                _bitmap.Clear(System.Windows.Media.Color.FromRgb(0, 0, 0));

                foreach (var triangle in model.Triangles)
                {
                    //Draw triangle
                    var n = (triangle.Normals[0] + triangle.Normals[1] + triangle.Normals[2]) / 3;

                    var dt = 0f;
                    foreach (var source in world.Lights)
                        dt += MathF.Max(Vector3.Dot(n, source), 0f);
                    dt = MathF.Min(dt, 1f);

                    var color = (int)(0xFF * dt) * 0x100 * 0x100 + (int)(0xFF * dt) * 0x100 + (int)(0xFF * dt);
                    DrawTriangle(triangle.Points[0], triangle.Points[1], triangle.Points[2], color);
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private void DrawTriangle(Vector4 v1, Vector4 v2, Vector4 v3,  int color)
        {
            v1.X = (int)MathF.Ceiling(v1.X); v1.Y = (int)MathF.Ceiling(v1.Y);
            v2.X = (int)MathF.Ceiling(v2.X); v2.Y = (int)MathF.Ceiling(v2.Y);
            v3.X = (int)MathF.Ceiling(v3.X); v3.Y = (int)MathF.Ceiling(v3.Y);
            if (v2.Y < v1.Y)
            {
                Vector4 tmp = v1;
                v1 = v2;
                v2 = tmp;
            }
            if (v3.Y < v1.Y)
            {
                Vector4 tmp = v3;
                v3 = v1;
                v1 = tmp;
            }
            if (v2.Y > v3.Y)
            {
                Vector4 tmp = v2;
                v2 = v3;
                v3 = tmp;
            }
            float dx13 = 0, dx12 = 0, dx23 = 0;
            if (v3.Y != v1.Y)
            {
                dx13 = v3.X - v1.X;
                dx13 /= v3.Y - v1.Y;
            }
            if (v2.Y != v1.Y)
            {
                dx12 = v2.X - v1.X;
                dx12 /= (v2.Y - v1.Y);
            }
            if (v3.Y != v2.Y)
            {
                dx23 = v3.X - v2.X;
                dx23 /= (v3.Y - v2.Y);
            }
            float wx1 = v1.X;
            float wx2 = wx1;
            float _dx13 = dx13;
            if (dx13 > dx12)
            {
                Utility.Swap(ref dx13, ref dx12);
            }
            // растеризуем верхний полутреугольник
            for (int y = (int)MathF.Ceiling(v1.Y); y < (int)MathF.Ceiling(v2.Y); y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                for (int x = (int)MathF.Ceiling(wx1); x <= (int)MathF.Ceiling(wx2); x++)
                {
                    if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, y) * 100;
                        if (z < _zBuffer[x * _height + y])
                        {
                            DrawPixel(x, y, color);
                            _zBuffer[x * _height + y] = z;
                        }
                    }
                }
                wx1 += dx13;
                wx2 += dx12;
            }
            // вырожденный случай, когда верхнего полутреугольника нет
            // надо разнести рабочие точки по оси x, т.к. изначально они совпадают
            if (v1.Y == v2.Y)
            {
                wx1 = v1.X < v2.X ? v1.X : v2.X;
                wx2 = v1.X >= v2.X ? v1.X : v2.X;
            }
            // упорядочиваем приращения
            // (используем сохраненное приращение)
            if (_dx13 < dx23)
            {
                float tmp = _dx13;
                _dx13 = dx23;
                dx23 = tmp;
            }
            // растеризуем нижний полутреугольник
            for (int y = (int)MathF.Ceiling(v2.Y); y <= (int)MathF.Ceiling(v3.Y); y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                for (int x = (int)MathF.Ceiling(wx1); x <= (int)MathF.Ceiling(wx2); x++)
                {
                    if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, y) * 100;
                        if (z < _zBuffer[x * _height + y])
                        {
                            DrawPixel(x, y, color);
                            _zBuffer[x * _height + y] = z;
                        }
                    }
                }
                wx1 += _dx13;
                wx2 += dx23;
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
