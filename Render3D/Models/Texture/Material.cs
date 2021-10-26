using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models.Texture
{
    public class Material
    {
        public string Name { get; set; }
        public Vector3 AmbientColor { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public Vector3 SpecularColor { get; set; }
        public float SpecularHighlights { get; set; }
        public float Dissolve { get; set; }
        public float IlluminationModel { get; set; }
        public string ColorTexture { get; set; }
    }
}
