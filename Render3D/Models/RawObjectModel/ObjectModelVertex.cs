using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public struct ObjectModelVertex
    {
        public int v;
        public int vt;
        public int vn;

        public Material material;
    }
}
