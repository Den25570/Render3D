using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class World
    {
        public Vector3[] Lights { get; set; }
        public Vector3[] LightsColors { get; set; }

        public float BackgroundLight { get; set; }

        public float MirrorLight { get; set; }

        public Camera Camera { get; set; }
    }
}
