using Render3D.Extensions;
using Render3D.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class Model
    {
        public Triangle[] Triangles;

        public Model() {}

        public Model(ObjectModel loadedModel)
        {
            FacesToTriangles(loadedModel);
        } 

        private void FacesToTriangles(ObjectModel loadedModel)
        {
            List<Triangle> triangles = new List<Triangle>();
            foreach (var face in loadedModel.Faces)
            {
                for(int i = 1; i < face.Count() - 1; i++)
                {
                    var v1 = loadedModel.Vertices[face[0].v - 1];
                    var v2 = loadedModel.Vertices[face[i].v - 1];
                    var v3 = loadedModel.Vertices[face[i + 1].v - 1];
                    triangles.Add(new Triangle()
                    {
                        Points = new Vector4[] { v1, v2, v3 },
                        Normal = loadedModel.VertexNormals[face[0].vn - 1]
                    });
                }
            }
            Triangles = triangles.ToArray();
        }

        public Model TransformModel(Matrix4x4 transform, bool transformNormals = false)
        {
            Model newModel = new Model()
            {
                Triangles = new Triangle[Triangles.Length]
            };

            for (int i = 0; i < Triangles.Length; i++)
            {
                newModel.Triangles[i] = new Triangle() { Points = new Vector4[3], Normal = Triangles[i].Normal };
                for (int j = 0; j < Triangles[i].Points.Length; j++)
                {
                    newModel.Triangles[i].Points[j] = Vector4.Transform(Triangles[i].Points[j], transform);
                    newModel.Triangles[i].Points[j] /= newModel.Triangles[i].Points[j].W;
                }
                if (transformNormals)
                {
                    newModel.Triangles[i].Normal = Vector3.TransformNormal(newModel.Triangles[i].Normal, transform);
                }
            }
            return newModel;
        }

        public Model RemoveHiddenFaces(Vector3 cameraPosition)
        {
            var visibleTriangles = new List<Triangle>();
            Model newModel = new Model() {};

            for (int i = 0; i < Triangles.Length; i++)
            {
                var n = Triangles[i].Normal;
                var v = Triangles[i].Points[0];
                if (Vector3.Dot(n, v.ToVector3() - cameraPosition) < 0)
                {
                    visibleTriangles.Add(new Triangle(Triangles[i])) ;
                }
            }
            newModel.Triangles = visibleTriangles.ToArray();
            return newModel;
        }

        public Model ClipTriangles(Vector3 plane, Vector3 planeNormal)
        {
            var triangles = new List<Triangle>();
            Model newModel = new Model() { };

            for (int i = 0; i < Triangles.Length; i++)
            {
                var newTriangles = Math3D.ClipTriangle(plane, planeNormal, Triangles[i]);
                triangles.AddRange(newTriangles);
            }

            newModel.Triangles = triangles.ToArray();
            return newModel;
        }
    }
}
