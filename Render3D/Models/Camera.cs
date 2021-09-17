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
        public Vector3 Target { get; set; }

        public Vector3 Forward { get { return Vector3.Normalize(Target - Position); } }
        public Vector3 Right { get { return Vector3.Normalize(Vector3.Cross(Up, Forward)); } }
        public Vector3 Up { get; set; }

        public void Move(Vector3 direction)
        {
            var forward = Forward;
            Position = Position + direction;
            Target = Position + forward;
        }

        public void Rotate(Vector3 angles)
        {
            //Matrix4x4 cameraRotation = Math.Math3D.GetRotationMatrix(angles);
            //Matrix4x4 cameraRotation = Math3D.GetRotationAroundVectorMatrix(Forward, angles);

            var forward = Vector3.Normalize(Math3D.RotateVectorAroundAxis(Forward, Up, angles.Y));
            var right = Vector3.Normalize(Vector3.Cross(Up, forward));


            forward = Vector3.Normalize(Math3D.RotateVectorAroundAxis(forward, right, angles.X));
            var up = Vector3.Normalize(Math3D.RotateVectorAroundAxis(Up, right, angles.X));
            //var forward = Vector3.Normalize(Vector3.Transform(Forward, cameraRotation));

            Up = up;
            Target = Position + forward;
        }
    }
}
