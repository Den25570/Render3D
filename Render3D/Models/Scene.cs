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
        public Vector3[] Lights { get; set; }
        public Vector3[] LightsColors { get; set; }

        public float BackgroundLightIntensity { get; set; }
        public float MirrorLightIntensity { get; set; }

        public Camera MainCamera { get; set; }
    }
}
