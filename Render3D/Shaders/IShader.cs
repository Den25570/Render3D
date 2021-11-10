using Render3D.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Shaders
{
    public interface IShader
    {
        public Vector3 GetColor(Vector3 position, Vector3 normal);
    }
}
