using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Extensions
{
    public static class VectorExtensions
    {
        public static Vector3 ToVector3(this Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static int ToRGB(this Vector3 v)
        {
            v = Vector3.Clamp(v, Vector3.Zero, Vector3.One);
            int color = ((int)(v.X*0xFF) * 0x100 * 0x100) + ((int)(v.Y * 0xFF) * 0x100) + (int)(v.Z * 0xFF);
            color = color > 0xFFFFFF ? 0xFFFFFF : color;
            color = color < 0 ? 0 : color;
            return color;
        }
    }
}
