using Render3D.Extensions;
using Render3D.Models;
using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class ObjectModel
    {
        public List<Vector4> Vertices { get; set; }
        public List<Vector3> VertexNormals { get; set; }
        public List<Vector2> TextureVertices { get; set; }
        public List<Vector3> SpaceVertices { get; set; }
        public List<List<ObjectModelVertex>> Faces { get; set; }
        public List<List<int>> Lines { get; set; }
        public List<Material> Materials { get; set; }

        public void CalculateNormals()
        {
            var normals = new List<Vector3> { };
            normals.AddRange(VertexNormals);
            for (int i = 0; i < Faces.Count; i++)
            {
                if (Faces[i][0].vn == 0)
                {
                    var l1 = Vertices[Faces[i][1].v - 1] - Vertices[Faces[i][0].v - 1];
                    var l2 = Vertices[Faces[i].Last().v - 1] - Vertices[Faces[i][0].v - 1];
                    var normal = Vector3.Normalize(Vector3.Cross(l1.ToVector3(), l2.ToVector3()));
                    normals.Add(normal);

                    var newFace = new List<ObjectModelVertex>();
                    foreach (var vertex in Faces[i])
                    {
                        newFace.Add(new ObjectModelVertex()
                        {
                            v = vertex.v,
                            vn = normals.Count(),
                            vt = vertex.vt
                        });
                    }
                    Faces[i] = newFace;
                }
            }
            VertexNormals = normals;
        }
    }
}
