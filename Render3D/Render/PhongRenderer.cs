using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Render3D.Render
{
    unsafe public class PhongRenderer : IRenderer
    {
        private WriteableBitmap _bitmap;
        float[] _zBuffer;
        SpinLock[] _zBufferSpinlock;
        private int _width;
        private int _height;

        private IntPtr _pBackBuffer;
        private int _backBufferStride;

        public bool HasBitmap { get => _bitmap != null; }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _zBuffer = new float[height * width];
            _zBufferSpinlock = new SpinLock[height * width];

            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            _backBufferStride = _bitmap.BackBufferStride;
            _pBackBuffer = _bitmap.BackBuffer;

            var image = new System.Windows.Controls.Image();
            image.Source = _bitmap;
            canvas.Children.Clear();
            canvas.Children.Add(image);
        }

        public void RenderModel(Model model, Scene world)
        {
            try
            {
                _bitmap.Lock();
                Array.Fill(_zBuffer, float.MaxValue);
                _bitmap.Clear(System.Windows.Media.Color.FromRgb(0, 0, 0));

                Parallel.For(0, model.Triangles.Length, (i) =>
                {
                    //Draw triangle
                    DrawTriangle(model.Triangles[i], world);
                });
                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private void DrawTriangle(Triangle triangle, Scene world)
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
                float tmp = dx13;
                dx13 = dx12;
                dx12 = tmp;
            }
            // растеризуем верхний полутреугольник
            for (int yi = (int)v1.Y; yi < (int)v2.Y; yi++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                float x = wx1;
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (xi >= 0 && yi >= 0 && xi < _width && yi < _height)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, yi);
                        int zIndex = xi * _height + yi;
                        if (z < _zBuffer[zIndex])
                        {
                            // Draw Pixel
                            var color = Math3D.InterpolateColor(triangle.Points[0], triangle.Points[1], triangle.Points[2], triangle.Colors[0], triangle.Colors[1], triangle.Colors[2], new Vector4(x, yi, 0, 1));
                            DrawPixel(xi, yi, color);
                            _zBuffer[zIndex] = z;
                            // Update z buffer
                            bool gotLock = false;
                            try
                            {
                                _zBufferSpinlock[zIndex].Enter(ref gotLock);
                                _zBuffer[zIndex] = z;
                            }
                            finally
                            {
                                if (gotLock) _zBufferSpinlock[zIndex].Exit();
                            }
                        }
                    }
                    x += 1;
                }
                wx1 += dx13;
                wx2 += dx12;
            }
            if (v1.Y == v2.Y)
            {
                wx1 = v1.X < v2.X ? v1.X : v2.X;
                wx2 = v1.X >= v2.X ? v1.X : v2.X;
            }
            if (_dx13 < dx23)
            {
                float tmp = _dx13;
                _dx13 = dx23;
                dx23 = tmp;
            }
            // растеризуем нижний полутреугольник
            for (int yi = (int)v2.Y; yi <= (int)v3.Y; yi++)
            {
                // рисуем горизонтальную линию между рабочими
                // точками
                float x = wx1;
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (x >= 0 && yi >= 0 && xi < _width && yi < _height)
                    {
                        var z = Math3D.InterpolateZ(v1, v2, v3, x, yi);
                        int zIndex = xi * _height + yi;
                        if (z < _zBuffer[zIndex])
                        {
                            // Draw Pixel
                            var color = Math3D.InterpolateColor(triangle.Points[0], triangle.Points[1], triangle.Points[2], triangle.Colors[0], triangle.Colors[1], triangle.Colors[2], new Vector4(x, yi, 0, 1));
                            DrawPixel(xi, yi, color);
                            _zBuffer[zIndex] = z;
                            // Update z buffer
                            bool gotLock = false;
                            try
                            {
                                _zBufferSpinlock[zIndex].Enter(ref gotLock);
                                _zBuffer[zIndex] = z;
                            }
                            finally
                            {
                                if (gotLock) _zBufferSpinlock[zIndex].Exit();
                            }
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
            Int32 offest = y * _backBufferStride + x * 4;
            *((int*)(_pBackBuffer + offest)) = color.ToRGB();
        }
    }
}
