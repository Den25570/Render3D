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
    unsafe public class TextureRenderer : IRenderer
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

                //Logic
                var worldModel = new Model(model);
                Matrix4x4.Invert(worldToPerspective, out var invWorldToPerspective);
                worldModel.TransformModel(invWorldToPerspective);

                Parallel.For(0, model.Triangles.Length, (i) =>
                {
                    //Draw triangle
                    DrawTriangle(model.Triangles[i], worldModel.Triangles[i], scene, modelToWorld);
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

        private void DrawTriangle(Triangle triangle, Triangle triangle3D, Scene scene, Matrix4x4 modelToWorld)
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

                    /*varying vec2 vN;

                        void main() {

                        vec4 p = vec4( position, 1. );

                        vec3 e = normalize( vec😔 modelViewMatrix * p ) );
                        vec3 n = normalize( normalMatrix * normal );

                        vec3 r = reflect( e, n );
                        float m = 2. * sqrt(
                        pow( r.x, 2. ) +
                        pow( r.y, 2. ) +
                        pow( r.z + 1., 2. )
                        );
                        vN = r.xy / m + .5;

                        gl_Position = projectionMatrix * modelViewMatrix * p;

}*/
                    _zBufferSpinlock[zIndex].Enter(ref gotLock);
                    if (zValue < _zBuffer[zIndex])
                    {
                        var coord = (barycenter.X * triangle.TextureCoordinates[pi[0]] + barycenter.Y * triangle.TextureCoordinates[pi[1]] + barycenter.Z * triangle.TextureCoordinates[pi[2]]);
                        coord = Vector2.Clamp(coord, Vector2.Zero, Vector2.One);

                        var pos = (barycenter.X * triangle3D.Points[pi[0]] + barycenter.Y * triangle3D.Points[pi[1]] + barycenter.Z * triangle3D.Points[pi[2]]).ToVector3();
                        var normal = (barycenter.X * triangle.Normals[pi[0]] + barycenter.Y * triangle.Normals[pi[1]] + barycenter.Z * triangle.Normals[pi[2]]);
                        //if (triangle.Material.NormalsMap != null) normal = Vector3.TransformNormal(triangle.Material.GetNormal(coord.X, 1 - coord.Y).Value, modelToWorld);
                        var ambientColor = triangle.Material.GetAmbientColor(coord.X, 1 - coord.Y);
                        var diffuseColor = triangle.Material.GetDiffuseColor(coord.X, 1 - coord.Y);
                        var specularColor = triangle.Material.GetSpecularColor(coord.X, 1 - coord.Y);
                        var specularHl = triangle.Material.GetSpecularHighlight(coord.X, 1 - coord.Y);

                        var color = (barycenter.X * triangle.Colors[pi[0]] + barycenter.Y * triangle.Colors[pi[1]] + barycenter.Z * triangle.Colors[pi[2]]) * 0.4f;
                        for (int li = 0; li < scene.Lights.Length; li++)
                        {
                            var l = Vector3.Normalize(scene.Lights[li] - pos);
                            var e = Vector3.Normalize(-pos);
                            var r = Vector3.Normalize(-Vector3.Reflect(l, normal));
                            Vector3 Iamb = ambientColor * scene.LightsColors[li] * scene.BackgroundLightIntensity;
                            Vector3 Idiff = diffuseColor * scene.LightsColors[li] * MathF.Max(Vector3.Dot(normal, l), 0.0f);
                            Idiff = Vector3.Clamp(Idiff, Vector3.Zero, Vector3.One);
                            Vector3 Ispec = specularColor * scene.LightsColors[li] * MathF.Pow(MathF.Max(Vector3.Dot(r, e), 0.0f), specularHl);
                            Ispec = Vector3.Clamp(Ispec, Vector3.Zero, Vector3.One);

                            color += (Iamb + Idiff + Ispec) * 0.6f;
                        }
                        //Reflection
                        if (triangle.Material.ReflectionMap != null)
                        {
                            var r = Vector3.Normalize(Vector3.Reflect(Vector3.Normalize(pos), Vector3.Normalize(normal)));
                            float m = 2.0f * System.MathF.Sqrt(
                                System.MathF.Pow(r.X, 2.0f ) +
                                System.MathF.Pow(r.Y, 2.0f ) +
                                System.MathF.Pow(r.Z + 1.0f, 2.0f )
                            );
                            var vN = new Vector2((r.X / m) + 0.5f, (r.Y / m) + 0.5f);
                            color += 0.4f * (triangle.Material.GetReflection(vN.X, 1- vN.Y) ?? Vector3.Zero);
                        }

                        _pixelBuffer[zIndex] = Vector3.Normalize(Vector3.Reflect(Vector3.Normalize(pos), Vector3.Normalize(normal))).ToRGB();
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
