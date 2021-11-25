using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Render3D.Render
{
    unsafe public class ZBufferRenderer : IRenderer
    {
        private float[] _zBuffer;
        private SpinLock[] _zBufferSpinlock;
        private int _width;
        private int _height;
        private int[] _pixelBuffer;

        public bool HasBitmap { get => true; }
        public float[] ZBuffer { get => _zBuffer; }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _zBuffer = new float[height * width];
            _pixelBuffer = new int[width * height];
            _zBufferSpinlock = new SpinLock[height * width];
        }

        public void RenderModel(Model viewModel, Model worldModel, Scene scene)
        {
            Array.Fill(_zBuffer, float.MaxValue);
            Array.Fill(_pixelBuffer, 0);

            Parallel.For(0, viewModel.Triangles.Length, (i) =>
            {
                DrawTriangle(viewModel.Triangles[i], worldModel.Triangles[i], scene);
            });
        }

        private void DrawTriangle(Triangle triangle, Triangle triangle3D, Scene scene)
        {
            triangle.Points = new Vector4[]
            {
                new Vector4((int)triangle.Points[0].X, (int)triangle.Points[0].Y, triangle.Points[0].Z, 0),
                new Vector4((int)triangle.Points[1].X, (int)triangle.Points[1].Y, triangle.Points[1].Z, 0),
                new Vector4((int)triangle.Points[2].X, (int)triangle.Points[2].Y, triangle.Points[2].Z, 0)
            };
            var pi = new List<int>() { 0,1 ,2};

            if (triangle.Points.Select((_, i) => triangle.Points[i].CompareXY(triangle.Points[(i + 1) % triangle.Points.Length])).All(b => b))
                return;

            Action<int, int> drawPixel = delegate (int xi, int yi)
            {
                var barycenter = Math3D.GetBarycenter(triangle.Points[pi[0]], triangle.Points[pi[1]], triangle.Points[pi[2]], xi, yi);
                barycenter = Vector3.Clamp(barycenter / (barycenter.X + barycenter.Y + barycenter.Z), Vector3.Zero, Vector3.One);
                barycenter /= (barycenter.X + barycenter.Y + barycenter.Z);

                var zValue = barycenter.X * triangle.Points[pi[0]].Z + barycenter.Y * triangle.Points[pi[1]].Z + barycenter.Z * triangle.Points[pi[2]].Z;
                var zIndex = xi * _height + yi;
                var gotLock = false;
                try
                {
                    _zBufferSpinlock[zIndex].Enter(ref gotLock);
                    if (zValue < _zBuffer[zIndex])
                    {
                        _pixelBuffer[zIndex] = new Vector3(zValue).ToRGB();
                        _zBuffer[zIndex] = zValue;
                    }
                }
                finally
                {
                    if (gotLock) _zBufferSpinlock[zIndex].Exit();
                }
            };

            pi.Sort((vx, vy) => triangle.Points[vx].Y > triangle.Points[vy].Y ? 1 : 0);
            var deltaVectors = new List<float>()
            {
                triangle.Points[pi[2]].Y != triangle.Points[pi[0]].Y ? (triangle.Points[pi[2]].X - triangle.Points[pi[0]].X) / (triangle.Points[pi[2]].Y - triangle.Points[pi[0]].Y) : 0, // dx13
                triangle.Points[pi[1]].Y != triangle.Points[pi[0]].Y ? (triangle.Points[pi[1]].X - triangle.Points[pi[0]].X) / (triangle.Points[pi[1]].Y - triangle.Points[pi[0]].Y) : 0, // dx12
                triangle.Points[pi[2]].Y != triangle.Points[pi[1]].Y ? (triangle.Points[pi[2]].X - triangle.Points[pi[1]].X) / (triangle.Points[pi[2]].Y - triangle.Points[pi[1]].Y) : 0, // dx23
            };


            float wx1 = triangle.Points[pi[0]].X;
            float wx2 = wx1;
            float _dx13 = deltaVectors[0];

            if (deltaVectors[0] > deltaVectors[1])
            {
                float tmp = deltaVectors[0];
                deltaVectors[0] = deltaVectors[1];
                deltaVectors[1] = tmp;
            }

            for (int yi = (int)triangle.Points[pi[0]].Y; yi < (int)triangle.Points[pi[1]].Y; yi++)
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

            if (triangle.Points[pi[0]].Y == triangle.Points[pi[1]].Y)
            {
                wx1 = System.Math.Min(triangle.Points[pi[0]].X, triangle.Points[pi[1]].X);
                wx2 = System.Math.Max(triangle.Points[pi[0]].X, triangle.Points[pi[1]].X);
            }
            if (_dx13 < deltaVectors[2])
            {
                float tmp = _dx13;
                _dx13 = deltaVectors[2];
                deltaVectors[2] = tmp;
            }

            for (int yi = (int)triangle.Points[pi[1]].Y; yi <= (int)triangle.Points[pi[2]].Y; yi++)
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
    }
}
