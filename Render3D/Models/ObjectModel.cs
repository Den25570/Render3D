using Render3D.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Model
{
    public class ObjectModel
    {
        public Vector4[] Vertices;
        public Vector3[] VertexNormals;
        public Vector3[] TextureVertices;
        public Vector3[] SpaceVertices;

        public List<Vector3>[] Faces;
        public List<int>[] Lines;

        public ObjectModel TransformModel(Matrix4x4 transform)
        {
            ObjectModel newModel = new ObjectModel()
            {
                Vertices = new Vector4[Vertices.Length],

                VertexNormals = VertexNormals,
                TextureVertices = TextureVertices,
                SpaceVertices = SpaceVertices,
                Faces = Faces,
                Lines = Lines,
            };

            for (int i = 0; i < Vertices.Length; i++)
            {
                newModel.Vertices[i] = Vertices[i];
                newModel.Vertices[i] = Vector4.Transform(newModel.Vertices[i], transform);
                newModel.Vertices[i].X /= newModel.Vertices[i].W;
                newModel.Vertices[i].Y /= newModel.Vertices[i].W;
                newModel.Vertices[i].Z /= newModel.Vertices[i].W;

            }
            return newModel;
        }
        public ObjectModel TranslateModel(Vector4 translateVector)
        {
            ObjectModel newModel = new ObjectModel()
            {
                Vertices = new Vector4[Vertices.Length],

                VertexNormals = VertexNormals,
                TextureVertices = TextureVertices,
                SpaceVertices = SpaceVertices,
                Faces = Faces,
                Lines = Lines,
            };

            for (int i = 0; i < Vertices.Length; i++)
            {
                newModel.Vertices[i] = Vector4.Add(Vertices[i], translateVector);
            }
            return newModel;
        }
    }
}
