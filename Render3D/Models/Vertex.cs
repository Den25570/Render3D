using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public struct Vertex
    {
        public Vector4 Position;
        public Vector3 Normal;
        public int Color;

        public Vertex(Vector4 position, Vector3 normal, int color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }
}
