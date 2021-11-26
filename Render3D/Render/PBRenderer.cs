using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models;
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
    unsafe public class PBRenderer : IRenderer
    {
        private WriteableBitmap _bitmap;
        private float[] _zBuffer;
        private SpinLock[] _zBufferSpinlock;
        private int _width;
        private int _height;
        private IntPtr _pBackBuffer;
        private int[] _pixelBuffer;
        private int _backBufferStride;

        private RenderOptions _renderOptions;

        public float[] ZBuffer { get => _zBuffer; }
        public bool HasBitmap { get => _bitmap != null; }

        public PBRenderer(RenderOptions renderOptions)
        {
            _renderOptions = renderOptions;
        }

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

        public void RenderModel(Model viewModel, Model worldModel, Scene scene)
        {
            try
            {
                _bitmap.Lock();
                Array.Fill(_zBuffer, float.MaxValue);
                Array.Fill(_pixelBuffer, 0);

                Parallel.For(0, viewModel.Triangles.Length, (i) =>
                {
                    DrawTriangle(viewModel.Triangles[i], worldModel.Triangles[i], scene);
                });
                WritePixelsToBitmap();

                _bitmap.AddDirtyRect(new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight));
            }
            finally
            {
                _bitmap.Unlock();
            }

        }

        private void DrawTriangle(Triangle triangle, Triangle triangle3D, Scene scene)
        {
            triangle.Points = new Vector4[]
            {
                new Vector4((int)triangle.Points[0].X, (int)triangle.Points[0].Y, triangle.Points[0].Z, 0),
                new Vector4((int)triangle.Points[1].X, (int)triangle.Points[1].Y, triangle.Points[1].Z, 0),
                new Vector4((int)triangle.Points[2].X, (int)triangle.Points[2].Y, triangle.Points[2].Z, 0)
            };
            var pi = new List<int>() { 0, 1, 2 };

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
                        var coord = (barycenter.X * triangle.TextureCoordinates[pi[0]] + barycenter.Y * triangle.TextureCoordinates[pi[1]] + barycenter.Z * triangle.TextureCoordinates[pi[2]]);
                        coord /= coord.Z;
                        coord = Vector3.Clamp(coord, Vector3.Zero, Vector3.One);
                        var pos = (barycenter.X * triangle3D.Points[pi[0]] + barycenter.Y * triangle3D.Points[pi[1]] + barycenter.Z * triangle3D.Points[pi[2]]).ToVector3();

                        var albedo = triangle.Material?.GetDiffuseColor(coord.X, 1 - coord.Y) ?? Vector3.One; //Ka
                        var specularColor = triangle.Material?.GetSpecularColor(coord.X, 1 - coord.Y) ?? Vector3.One; //Ks
                        var roughness = triangle.Material?.GetSpecularHighlight(coord.X, 1 - coord.Y, 1 / triangle.Material.SpecularHighlights) ?? 0.5f; //Ns
                        var metallic = (triangle.Material?.GetReflection(coord.X, 1 - coord.Y) ?? Vector3.Zero).X; //refl

                        // normal mapping
                        var normal = Vector3.Normalize(barycenter.X * triangle.Normals[pi[0]] + barycenter.Y * triangle.Normals[pi[1]] + barycenter.Z * triangle.Normals[pi[2]]);
                        if (triangle.Material?.NormalsMap != null)
                        {
                            //normal = (triangle.Normals[pi[1]] + triangle.Normals[pi[2]] + triangle.Normals[pi[0]]) / 3;
                            var tbn = Math3D.GetTriangleTBNMatrix(triangle3D, normal);
                            normal = triangle.Material.GetNormal(coord.X, 1 - coord.Y).Value;
                            normal = Vector3.Normalize(Vector3.Transform(normal, tbn));
                        }

                        var Lo = Vector3.Zero;
                        var v = Vector3.Normalize(scene.MainCamera.Position - pos);
                        var shadow = 0.0f;
                        if (_renderOptions.ShowShadows)
                            shadow = 1.0f;
                        for (int li = 0; li < scene.Lights.Length; li++)
                        {
                            var lightPos = scene.Lights[li].ToVector3();
                            var l = Vector3.Normalize(lightPos - pos);
                            var h = Vector3.Normalize(l + v);

                            var distance = (lightPos - pos).Length() / 50;
                            var attenuation = 1.0f / (distance * distance);
                            var radiance = scene.LightsColors[li] * attenuation;

                            var F0 = new Vector3(0.04f);
                            F0 = Vector3.Lerp(F0, albedo, metallic);
                            var F = fresnelSchlick(System.Math.Max(Vector3.Dot(h, v), 0.0f), F0);

                            float NDF = distributionGGX(normal, h, roughness);
                            float G = geometrySmith(normal, v, l, roughness);

                            var numerator = NDF * G * F;
                            var NdotL = System.Math.Max(Vector3.Dot(normal, l), 0.0f);
                            var denominator = 4.0f * System.Math.Max(Vector3.Dot(normal, v), 0.0f) * NdotL + 0.001f;
                            var specular = numerator / denominator;

                            var kS = F;
                            var kD = Vector3.One - kS;
                            kD *= 1.0f - metallic;

                            Lo += (kD * albedo / MathF.PI + specular) * Vector3.Abs(radiance) * NdotL;

                            //shadows
                            var projCoords = Vector4.Transform(new Vector4(pos.X, pos.Y, pos.Z, 1), scene.lightViewProjMatrixes[li]);
                            projCoords /= projCoords.W;
                            float bias = System.Math.Max(0.050f * (1.0f - Vector3.Dot(normal, l)), 0.0050f);
                            var localShadow = 0.0f;
                            for (int x = -1; x <= 1; x++)
                                for (int y = -1; y <= 1; y++)
                                    localShadow += projCoords.Z - bias > scene.shadowBuffers[li][(int)(projCoords.X + x), (int)(projCoords.Y + y)] ? 1.0f : 0.0f;
                            shadow = MathF.Min(shadow, localShadow / 9.0f);
                        }
                        var ambient = new Vector3(0.03f) * albedo;
                        var color = ambient + Lo * (1 - shadow);
                        color = color / (color + Vector3.One);
                        var pow = 1.0f / 1.6f;
                        color = new Vector3(MathF.Pow(color.X, pow), MathF.Pow(color.Y, pow), MathF.Pow(color.Z, pow));

                        if (_renderOptions.NormalsMode)
                            _pixelBuffer[zIndex] = normal.ToRGB();
                        else
                            _pixelBuffer[zIndex] = color.ToRGB();
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

        private Vector3 fresnelSchlick(float cosTheta, Vector3 F0)
        {
            return F0 + (Vector3.One - F0) * System.MathF.Pow(1.0f - cosTheta, 5.0f);
        }

        private float distributionGGX(Vector3 N, Vector3 H, float roughness)
        {
            float a = roughness * roughness;
            float a2 = a * a;
            float NdotH = System.Math.Max(Vector3.Dot(N, H), 0.0f);
            float NdotH2 = NdotH * NdotH;

            float num = a2;
            float denom = (NdotH2 * (a2 - 1.0f) + 1.0f);
            denom = MathF.PI * denom * denom;

            return num / denom;
        }

        private float geometrySchlickGGX(float NdotV, float roughness)
        {
            float r = (roughness + 1.0f);
            float k = (r * r) / 8.0f;
            float num = NdotV;
            float denom = NdotV * (1.0f - k) + k;
            return num / denom;
        }
        private float geometrySmith(Vector3 N, Vector3 V, Vector3 L, float roughness)
        {
            float NdotV = System.Math.Max(Vector3.Dot(N, V), 0.0f);
            float NdotL = System.Math.Max(Vector3.Dot(N, L), 0.0f);
            float ggx2 = geometrySchlickGGX(NdotV, roughness);
            float ggx1 = geometrySchlickGGX(NdotL, roughness);
            return ggx1 * ggx2;
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

