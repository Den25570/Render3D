using Render3D.Models;
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
            ObjectModel model = new ObjectModel()
            {

                Vertices = new List<Vector4>(),
                VertexNormals = new List<Vector3>(),
                TextureVertices = new List<Vector3>(),
                SpaceVertices = new List<Vector3>(),
                Faces = new List<List<ObjectModelVertex>>(),
                Lines = new List<List<int>>(),
            };


            using (StreamReader reader = File.OpenText(path))
            {
                for (string stringLine; (stringLine = reader.ReadLine()) != null;)
                {
                    List<string> items = stringLine.Replace('.', ',').Split(' ').ToList();
                    items.RemoveAll(s => s == "");

                    if (items.Count > 0)
                    {
                        switch (items[0])
                        {
                            case "v":
                                model.Vertices.Add(new Vector4()
                                {
                                    X = float.Parse(items[1]),
                                    Y = float.Parse(items[2]),
                                    Z = float.Parse(items[3]),
                                    W = items.Count >= 5 ? float.Parse(items[4]) : 1.0F,
                                });
                                break;
                            case "vt":
                                model.TextureVertices.Add(new Vector3()
                                {
                                    X = float.Parse(items[1]),
                                    Y = items.Count >= 3 ? float.Parse(items[2]) : 0.0F,
                                    Z = items.Count >= 4 ? float.Parse(items[3]) : 0.0F,
                                });
                                break;
                            case "vn":
                                model.VertexNormals.Add(new Vector3()
                                {
                                    X = float.Parse(items[1]),
                                    Y = float.Parse(items[2]),
                                    Z = float.Parse(items[3]),
                                });
                                break;
                            case "vp":
                                model.SpaceVertices.Add(new Vector3()
                                {
                                    X = float.Parse(items[1]),
                                    Y = items.Count >= 3 ? float.Parse(items[2]) : 0.0F,
                                    Z = items.Count >= 4 ? float.Parse(items[3]) : 0.0F,
                                });
                                break;
                            case "f":
                                var face = new List<ObjectModelVertex>();
                                for (int i = 1; i < items.Count; i++)
                                {
                                    if (items[i] != "")
                                    {
                                        var vertexIndices = items[i].Split('/');

                                        face.Add(new ObjectModelVertex()
                                        {
                                            v = int.Parse(vertexIndices[0]),
                                            vt = vertexIndices.Length > 1 && vertexIndices[1] != "" ? int.Parse(vertexIndices[1]) : 0,
                                            vn = vertexIndices.Length > 2 ? int.Parse(vertexIndices[2]) : 0,
                                        });
                                    }
                                }
                                model.Faces.Add(face);
                                break;
                            case "l":
                                var line = new List<int>();
                                for (int i = 1; i < items.Count; i++)
                                {
                                    line.Add(int.Parse(items[i]));
                                }
                                model.Lines.Add(line);
                                break;
                        }
                    }
                }
            }

            return model;
        }
    }
}
