using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
using Render3D.Models.Texture;
using Render3D.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private float[] _zBuffer;
        private SpinLock[] _zBufferSpinlock;
        private int _width;
        private int _height;
        private IntPtr _pBackBuffer;
        private int[] _pixelBuffer;
        private int _backBufferStride;

        public bool HasBitmap { get => _bitmap != null; }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _zBuffer = new float[height * width];
            _pixelBuffer = new int[width * height];
            _zBufferSpinlock = new SpinLock[height * width];

            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            _backBufferStride = _bitmap.BackBufferStride;
            _pBackBuffer = _bitmap.BackBuffer;

            var image = new Image();
            image.Source = _bitmap;
            canvas.Children.Clear();
            canvas.Children.Add(image);
        }

        public void RenderModel(Model model, Matrix4x4 modelToWorld, Matrix4x4 worldToPerspective, Scene scene)
        {
            try
            {
                _bitmap.Lock();
                Array.Fill(_zBuffer, float.MaxValue);
                Array.Fill(_pixelBuffer, 0);
                _bitmap.Clear(Color.FromRgb(0, 0, 0));

                Parallel.For(0, model.Triangles.Length, (i) =>
                {
                    //Draw triangle
                    DrawTriangle(model.Triangles[i]);
                });
                //DumbPixelFilter();
                WritePixelsToBitmap();

                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private void DrawTriangle(Triangle triangle)
        {
            var vectors = new List<Vector4>()
            {
                new Vector4((int)triangle.Points[0].X, (int)triangle.Points[0].Y, triangle.Points[0].Z, 0),
                new Vector4((int)triangle.Points[1].X, (int)triangle.Points[1].Y, triangle.Points[1].Z, 0),
                new Vector4((int)triangle.Points[2].X, (int)triangle.Points[2].Y, triangle.Points[2].Z, 0)
            };

            if (vectors.Select((_, i) => vectors[i].CompareXY(vectors[(i + 1) % vectors.Count])).All(b => b))
                return;

            Action<int, int> drawPixel = delegate (int xi, int yi)
            {
                var barycenter = Math3D.GetBarycenter(triangle.Points[0], triangle.Points[1], triangle.Points[2], xi, yi);
                barycenter = Vector3.Clamp(barycenter / (barycenter.X + barycenter.Y + barycenter.Z), Vector3.Zero, Vector3.One);
                barycenter /= (barycenter.X + barycenter.Y + barycenter.Z);

                var zValue = barycenter.X * triangle.Points[0].Z + barycenter.Y * triangle.Points[1].Z + barycenter.Z * triangle.Points[2].Z;
                var zIndex = xi * _height + yi;
                var gotLock = false;
                try
                {
                    _zBufferSpinlock[zIndex].Enter(ref gotLock);
                    if (zValue < _zBuffer[zIndex])
                    {
                        _pixelBuffer[zIndex] = (barycenter.X * triangle.Colors[0] + barycenter.Y * triangle.Colors[1] + barycenter.Z * triangle.Colors[2]).ToRGB();
                        _zBuffer[zIndex] = zValue;
                    }
                }
                finally
                {
                    if (gotLock) _zBufferSpinlock[zIndex].Exit();
                }
            };

            vectors.Sort((vx, vy) => vx.Y > vy.Y ? 1 : 0);
            var deltaVectors = new List<float>()
            {
                vectors[2].Y != vectors[0].Y ? (vectors[2].X - vectors[0].X) / (vectors[2].Y - vectors[0].Y) : 0, // dx13
                vectors[1].Y != vectors[0].Y ? (vectors[1].X - vectors[0].X) / (vectors[1].Y - vectors[0].Y) : 0, // dx12
                vectors[2].Y != vectors[1].Y ? (vectors[2].X - vectors[1].X) / (vectors[2].Y - vectors[1].Y) : 0, // dx23
            };

 
            float wx1 = vectors[0].X;
            float wx2 = wx1;
            float _dx13 = deltaVectors[0];

            if (deltaVectors[0] > deltaVectors[1])
            {
                float tmp = deltaVectors[0];
                deltaVectors[0] = deltaVectors[1];
                deltaVectors[1] = tmp;
            }

            for (int yi = (int)vectors[0].Y; yi < (int)vectors[1].Y; yi++)
            {
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (xi >= 0 && yi >= 0 && xi < _width && yi < _height)
                    {
                        drawPixel(xi, yi);
                    }
                }
                wx1 += deltaVectors[0];
                wx2 += deltaVectors[1];
            }

            if (vectors[0].Y == vectors[1].Y)
            {
                wx1 = vectors[0].X < vectors[1].X ? vectors[0].X : vectors[1].X;
                wx2 = vectors[0].X >= vectors[1].X ? vectors[0].X : vectors[1].X;
            }
            if (_dx13 < deltaVectors[2])
            {
                float tmp = _dx13;
                _dx13 = deltaVectors[2];
                deltaVectors[2] = tmp;
            }

            for (int yi = (int)vectors[1].Y; yi <= (int)vectors[2].Y; yi++)
            {
                for (int xi = (int)MathF.Round(wx1); xi <= (int)MathF.Round(wx2); xi++)
                {
                    if (xi >= 0 && yi >= 0 && xi < _width && yi < _height)
                    {
                        drawPixel(xi, yi);
                    }
                }
                wx1 += _dx13;
                wx2 += deltaVectors[2];
            }
        }

        private void PutPixel(int x, int y, Vector3 color)
        {
            Int32 offest = y * _backBufferStride + x * 4;
            *((int*)(_pBackBuffer + offest)) = color.ToRGB();
        }

        private void WritePixelsToBitmap()
        {
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    Int32 offest = y * _backBufferStride + x * 4;
                    *((int*)(_pBackBuffer + offest)) = _pixelBuffer[x * _height + y];
                }
            }
        }
    }
}
