using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class Scene
    {
        // World stuff
        public Vector4[] Lights { get; set; }
        public Vector3[] LightsColors { get; set; }
        public float BackgroundLightIntensity { get; set; }
        public float MirrorLightIntensity { get; set; }
        public Camera MainCamera { get; set; }

        //Rendering
        public List<Map<float>> shadowBuffers { get; set; }
        public List<Matrix4x4> lightViewProjMatrixes { get; set; }

        public Scene() { }

        public Scene(Scene scene)
        {
            Lights = (Vector4[])scene.Lights.Clone();
            LightsColors = (Vector3[])scene.LightsColors.Clone();
            MainCamera = scene.MainCamera;
            BackgroundLightIntensity = scene.BackgroundLightIntensity;
        }

        public void TransformLights(Matrix4x4 matrix)
        {
            Lights = Lights.Select(l => Vector4.Transform(l, matrix)).ToArray();
        }
    }
}
