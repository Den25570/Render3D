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
        public Vector3 Target { get; set; }

        public Vector3 Forward { get { return Vector3.Normalize(Target - Position); } }
        public Vector3 Right { get { return Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Forward)); } }
        public Vector3 Up { get { return Vector3.Cross(Forward, Right); } }

        public void Rotate(Vector3 angles)
        {
            Matrix4x4 cameraRotation = Math.Math3D.GetRotationMatrix(angles);
            var forward = Vector3.Transform(Vector3.UnitZ, cameraRotation);
            Target = Position + forward;
        }
    }
}
