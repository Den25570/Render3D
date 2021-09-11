using Render3D.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Parser
{
    public class OBJParser : IParser
    {
        public ObjectModel Parse(string path)
        {
            ObjectModel model = new ObjectModel();

            var Vertices = new List<Vector4>();
            var VertexNormals = new List<Vector3>();
            var TextureVertices = new List<Vector3>();
            var SpaceVertices = new List<Vector3>();
            var Faces = new List<List<Vector3>>();
            var Lines = new List<List<int>>();

            using (StreamReader reader = File.OpenText(path))
            {
                for (string stringLine; (stringLine = reader.ReadLine()) != null;)
                {
                    string[] items = stringLine.Split(' ');

                    if (items.Length > 0)
                    {
                        switch (items[0])
                        {
                            case "v":
                                Vertices.Add(new Vector4()
                                {
                                    X = float.Parse(items[1].Replace('.', ',')),
                                    Y = float.Parse(items[2].Replace('.', ',')),
                                    Z = float.Parse(items[3].Replace('.', ',')),
                                    W = items.Length >= 5 ? float.Parse(items[4].Replace('.', ',')) : 1.0F,
                                });
                                break;
                            case "vt":
                                TextureVertices.Add(new Vector3()
                                {
                                    X = float.Parse(items[1].Replace('.', ',')),
                                    Y = items.Length >= 3 ? float.Parse(items[2].Replace('.', ',')) : 0.0F,
                                    Z = items.Length >= 4 ? float.Parse(items[3].Replace('.', ',')) : 0.0F,
                                });
                                break;
                            case "vn":
                                VertexNormals.Add(new Vector3()
                                {
                                    X = float.Parse(items[1].Replace('.', ',')),
                                    Y = float.Parse(items[2].Replace('.', ',')),
                                    Z = float.Parse(items[3].Replace('.', ',')),
                                });
                                break;
                            case "vp":
                                SpaceVertices.Add(new Vector3()
                                {
                                    X = float.Parse(items[1].Replace('.', ',')),
                                    Y = items.Length >= 3 ? float.Parse(items[2].Replace('.', ',')) : 0.0F,
                                    Z = items.Length >= 4 ? float.Parse(items[3].Replace('.', ',')) : 0.0F,
                                });
                                break;
                            case "f":
                                var face = new List<Vector3>();
                                for (int i = 1; i < items.Length; i++)
                                {
                                    var vertexIndices = items[i].Split('/');
                                    face.Add(new Vector3()
                                    {
                                       X = int.Parse(vertexIndices[0]),
                                       Y = vertexIndices.Length >= 1 && vertexIndices[1] != "" ? int.Parse(vertexIndices[1]) : 0,
                                       Z = vertexIndices.Length >= 2 ? int.Parse(vertexIndices[2]) : 0,
                                    });
                                }
                                Faces.Add(face);
                                break;
                            case "l":
                                var line = new List<int>();
                                for (int i = 1; i < items.Length; i++)
                                {
                                    line.Add(int.Parse(items[i]));
                                }
                                Lines.Add(line);
                                break;
                        }
                    }
                }

                model.Vertices = Vertices.ToArray();
                model.VertexNormals = VertexNormals.ToArray();
                model.TextureVertices = TextureVertices.ToArray();
                model.SpaceVertices = SpaceVertices.ToArray();
                model.Faces = Faces.ToArray();
                model.Lines = Lines.ToArray();
            }

            return model;
        }
    }
}
