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
    public class PhongRenderer : IRenderer
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

                for(int i = 0; i < model.Triangles.Length; i++)
                {
                    //Draw triangle
                    DrawTriangle(model.Triangles[i], world);
                }
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private void DrawTriangle(Triangle triangle, World world)
        {
            triangle.Points[0].X = (int)triangle.Points[0].X; triangle.Points[0].Y = (int)triangle.Points[0].Y;
            triangle.Points[1].X = (int)triangle.Points[1].X; triangle.Points[1].Y = (int)triangle.Points[1].Y;
            triangle.Points[2].X = (int)triangle.Points[2].X; triangle.Points[2].Y = (int)triangle.Points[2].Y;
            var v1 = triangle.Points[0];
            var v2 = triangle.Points[1]; 
            var v3 = triangle.Points[2]; 
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
            for (int y = (int)v1.Y; y < (int)v2.Y; y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                float x = wx1;
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (xi >= 0 && y >= 0 && xi < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, y);
                        if (z <= _zBuffer[xi * _height + y])
                        {
                            var color = Math3D.InterpolateColor(triangle.Points[0], triangle.Points[1], triangle.Points[2], triangle.Colors[0], triangle.Colors[1], triangle.Colors[2], new Vector4(x, y, 0, 1));
                            DrawPixel(xi, y, color);
                            _zBuffer[xi * _height + y] = z;
                        }
                    }
                    x += 1;
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
            for (int y = (int)v2.Y; y <= (int)v3.Y; y++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                float x = wx1;
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (x >= 0 && y >= 0 && x < _bitmap.PixelWidth && y < _bitmap.PixelHeight)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, y);
                        if (z < _zBuffer[xi * _height + y])
                        {
                            var color = Math3D.InterpolateColor(triangle.Points[0], triangle.Points[1], triangle.Points[2], triangle.Colors[0], triangle.Colors[1], triangle.Colors[2], new Vector4(x, y, 0, 1));

                            DrawPixel(xi, y, color);
                            _zBuffer[xi * _height + y] = z;
                        }
                    }
                    x += 1;
                }
                wx1 += _dx13;
                wx2 += dx23;
            }
        }

        private void DrawPixel(int x, int y, Vector3 color)
        {
            unsafe
            {
                IntPtr pBackBuffer = _bitmap.BackBuffer;
                pBackBuffer += y * _bitmap.BackBufferStride;
                pBackBuffer += x * 4;
                *((int*)pBackBuffer) = color.ToRGB();
            }
        }
    }
}
