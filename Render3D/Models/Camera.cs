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
        public float Speed { get; set; }
        public float RotationSpeed { get; set; }
        public float MouseSpeed { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }

        public Vector3 Forward { get { return Vector3.Normalize(Position - Target); } }
        public Vector3 Right { get { return Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Forward)); } }
        public Vector3 Up { get { return Vector3.Cross(Forward, Right); } }

        public void TurnRight(float units)
        {
            Target = Position + Vector3.Normalize(Forward + (Right * units));
        }

        public void TurnLeft(float units)
        {
            Target = Position + Vector3.Normalize(Forward + (-Right * units));
        }

        public void TurnUp(float units)
        {
            Target = Position + Vector3.Normalize(Forward + (Up * units));
        }

        public void TurnDown(float units)
        {
            Target = Position + Vector3.Normalize(Forward + (-Up * units));
        }
    }
}
