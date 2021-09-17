﻿using System;
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

        public Vector3 Normal;

        public Triangle() { }

        public Triangle(Triangle triangle)
        {
            Points = (Vector4[])triangle.Points.Clone();
            Normal = triangle.Normal;
        }
    }
}