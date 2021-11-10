using Render3D.Extensions;
using Render3D.Math;
using Render3D.Models.Texture;
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
        public Triangle[] Triangles { get; private set; }
        public List<Material> Materials { get; private set; }

        public Model() {}

        public Model(ObjectModel loadedModel)
        {
            FacesToTriangles(loadedModel);
            Materials = loadedModel.Materials;
        }

        public Model(Model model)
        {
            if (model != null && model.Triangles != null)
            {
                Materials = model.Materials;
                Triangles = new Triangle[model.Triangles.Length];
                for(int i = 0; i < Triangles.Length; i++)
                {
                    Triangles[i] = new Triangle(model.Triangles[i]);
                }
            }
        }

        // TODO Add lights color
        // TODO Add material/texture
        public void CalculateColor(Scene world)
        {
            Parallel.For(0, Triangles.Length, (i) =>
            {
                for(int j = 0; j < Triangles[i].Points.Length; j++)
                {
                    var result = new Vector3();
                    for (int li = 0; li < world.Lights.Length; li++)
                    {
                        var l = Vector3.Normalize(world.Lights[li] - Triangles[i].Points[j].ToVector3());
                        var e = Vector3.Normalize(-Triangles[i].Points[j].ToVector3());
                        var r = Vector3.Normalize(-Vector3.Reflect(l, Triangles[i].Normals[j]));

                        Vector3 Iamb = Triangles[i].Colors[j] * world.BackgroundLightIntensity;

                        Vector3 Idiff = Triangles[i].Colors[j] * MathF.Max(Vector3.Dot(Triangles[i].Normals[j], l), 0.0f);
                        Idiff = Vector3.Clamp(Idiff, Vector3.Zero, Vector3.One);

                        Vector3 Ispec = world.LightsColors[li] * MathF.Pow(MathF.Max(Vector3.Dot(r, e), 0.0f), 5);
                        Ispec = Vector3.Clamp(Ispec, Vector3.Zero, Vector3.One);

                        result += Iamb + Idiff + Ispec;
                    }
                    Triangles[i].Colors[j] = result;
                }
            });
        }

        private void FacesToTriangles(ObjectModel loadedModel)
        {
            List<Triangle> triangles = new List<Triangle>();
            for (int i = 0; i < loadedModel.Faces.Count(); i++)
            {
                for(int j = 0; j < loadedModel.Faces[i].Count() - 1; j++)
                {

                    var v1 = loadedModel.Vertices[loadedModel.Faces[i][0].v - 1];
                    var v2 = loadedModel.Vertices[loadedModel.Faces[i][j].v - 1];
                    var v3 = loadedModel.Vertices[loadedModel.Faces[i][j + 1].v - 1];

                    var n1 = loadedModel.VertexNormals[loadedModel.Faces[i][0].vn - 1];
                    var n2 = loadedModel.VertexNormals[loadedModel.Faces[i][j].vn - 1];
                    var n3 = loadedModel.VertexNormals[loadedModel.Faces[i][j + 1].vn - 1];

                    var vt1 = loadedModel.Faces[i][0].vt != 0 ? loadedModel.TextureVertices[loadedModel.Faces[i][0].vt - 1] : new Vector2();
                    var vt2 = loadedModel.Faces[i][0].vt != 0 ? loadedModel.TextureVertices[loadedModel.Faces[i][j].vt - 1] : new Vector2();
                    var vt3 = loadedModel.Faces[i][0].vt != 0 ? loadedModel.TextureVertices[loadedModel.Faces[i][j + 1].vt - 1] : new Vector2();
                    triangles.Add(new Triangle()
                    {
                        TextureCoordinates = new Vector2[] { vt1 , vt2, vt3 },
                        Points = new Vector4[] { v1, v2, v3 },
                        Normals = new Vector3[] { n1, n2, n3 },
                        Colors = new Vector3[3] { Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ }
                    });;
                }
            }
            Triangles = triangles.ToArray();
        }

        public void TransformModel(Matrix4x4 transform, bool transformNormals = false)
        {
            Parallel.For(0, Triangles.Length, (i) =>
            {
                for (int j = 0; j < Triangles[i].Points.Length; j++)
                {
                    Triangles[i].Points[j] = Vector4.Transform(Triangles[i].Points[j], transform);
                    Triangles[i].Points[j] /= Triangles[i].Points[j].W;
                    Triangles[i].TextureCoordinates[j] /= Triangles[i].Points[j].W;
                }
                if (transformNormals)
                {
                    for (int j = 0; j < Triangles[i].Normals.Length; j++)
                    {
                        Triangles[i].Normals[j] = Vector3.TransformNormal(Triangles[i].Normals[j], transform);
                    }
                }
            });
        }

        public void RemoveHiddenFaces(Vector3 cameraPosition)
        {
            var visibleTriangles = new List<Triangle>();
            for (int i = 0; i < Triangles.Length; i++)
            {
                if (Vector3.Dot(Triangles[i].Normals[0], Triangles[i].Points[0].ToVector3() - cameraPosition) < 0 || 
                    Vector3.Dot(Triangles[i].Normals[1], Triangles[i].Points[1].ToVector3() - cameraPosition) < 0 || 
                    Vector3.Dot(Triangles[i].Normals[2], Triangles[i].Points[2].ToVector3() - cameraPosition) < 0)
                {
                    visibleTriangles.Add(new Triangle(Triangles[i])) ;
                }
            }
            Triangles = visibleTriangles.ToArray();
        }

        public void ClipTriangles(Vector3 plane, Vector3 planeNormal)
        {
            var triangles = new List<Triangle>();
            for (int i = 0; i < Triangles.Length; i++)
            {
                var newTriangles = Math3D.ClipTriangle(plane, planeNormal, Triangles[i]);
                triangles.AddRange(newTriangles);
            }
            Triangles = triangles.ToArray();
        }
    }
}
