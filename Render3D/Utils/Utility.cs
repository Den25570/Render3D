using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Utils
{
    public static class Utility
    {
        public static void Swap(ref int a, ref int b)
        {
            int t = a;
            a = b;
            b = t;
        }
        public static void Swap(ref float a, ref float b)
        {
            float t = a;
            a = b;
            b = t;
        }

        public static void Swap(ref Vector4 a, ref Vector4 b)
        {
            Vector4 t = a;
            a = b;
            b = t;
        }
    }
}
