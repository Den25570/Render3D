using Render3D.Extensions;
using Render3D.Models;
using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Shaders
{
    public class PhongShader : IShader
    {
        private Scene _scene;
        private Material _material;

        public PhongShader(Scene scene, Material material)
        {
            _scene = scene;
            _material = material;
        }

        public Vector3 GetColor(Vector3 position, Vector3 normal)
        {
            //Temp color
            var color = new Vector3(0, 1, 1);

            var result = new Vector3();
            for (int li = 0; li < _scene.Lights.Length; li++)
            {
                var l = Vector3.Normalize(_scene.Lights[li].ToVector3() - position);
                var e = Vector3.Normalize(-position);
                var r = Vector3.Normalize(-Vector3.Reflect(l, normal));

                Vector3 Iamb = color * _scene.BackgroundLightIntensity;

                Vector3 Idiff = color * MathF.Max(Vector3.Dot(normal, l), 0.0f);
                Idiff = Vector3.Clamp(Idiff, Vector3.Zero, Vector3.One);

                Vector3 Ispec = _scene.LightsColors[li] * MathF.Pow(MathF.Max(Vector3.Dot(r, e), 0.0f), 5);
                Ispec = Vector3.Clamp(Ispec, Vector3.Zero, Vector3.One);

                result += Iamb + Idiff + Ispec;
            }
            return result;
        }
    }
}
