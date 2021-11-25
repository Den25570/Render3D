using Render3D.Math;
using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class Triangle
    {
        public Vector4[] Points;

        public Vector3[] Normals;

        public Vector3[] Colors;

        public Vector3[] TextureCoordinates;

        public Material Material;

        public Triangle() { }

        public Triangle(Triangle triangle)
        {
            Material = triangle.Material;
            Points = (Vector4[])triangle.Points.Clone();
            Normals = (Vector3[])triangle.Normals.Clone();
            Colors = (Vector3[])triangle.Colors.Clone();
            TextureCoordinates = (Vector3[])triangle.TextureCoordinates.Clone();
        }
    }
}
