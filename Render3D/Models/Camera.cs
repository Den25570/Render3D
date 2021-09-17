using Render3D.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class Camera
    {
        public float FOV { get; set; }
        public float ZNear { get; set; }
        public float ZFar { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }

        public Camera()
        {
            FOV = MathF.PI / 2;
            ZNear = 0.1f;
            ZFar = 200f;
            Position = Vector3.UnitZ;
            Rotation = Vector3.Zero;
        }
    }
}
