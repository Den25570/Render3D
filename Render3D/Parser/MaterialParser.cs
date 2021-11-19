using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Parser
{
    public class MaterialParser : IParser
    {
        public object Parse(string path)
        {
            List<Material> materials = new List<Material>();
            int materialsArrayLength = 0;
            Material material = null;

            if (File.Exists(path))
            using (StreamReader reader = File.OpenText(path))
            {
                List<Task> TaskList = new List<Task>();
                for (string stringLine; (stringLine = reader.ReadLine()) != null;)
                {
                    List<string> items = stringLine.Replace('.', ',').Split(' ').ToList();
                    items.RemoveAll(s => s == "");

                    if (items.Count > 0)
                            switch (items[0])
                            {
                                // Material color and illumination statements:
                                case "newmtl":
                                    Task.WaitAll(TaskList.ToArray());
                                    material = new Material();
                                    material.Name = items[1];
                                    materials.Add(material);
                                    materialsArrayLength++;
                                    break;
                                case "Ka":
                                    material.AmbientColor = new System.Numerics.Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                                    break;
                                case "Kd":
                                    material.DiffuseColor = new System.Numerics.Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                                    break;
                                case "Ks":
                                    material.SpecularColor = new System.Numerics.Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                                    break;
                                case "Tf":
                                    material.TransmissionFilter = new System.Numerics.Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3]));
                                    break;
                                case "illum":
                                    material.IlluminationModel = float.Parse(items[1]);
                                    break;
                                case "Ns":
                                    material.SpecularHighlights = float.Parse(items[1]);
                                    break;
                                case "Ni":
                                    material.OpticalDensity = float.Parse(items[1]);
                                    break;
                                case "d":
                                    material.Dissolve = float.Parse(items[1]);
                                    break;
                                case "sharpness":
                                    material.Sharpness = float.Parse(items[1]);
                                    break;

                                case "map_Ka":
                                    var bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    var task = new Task(() => { material.AmbientColorMap = material.ImageToMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "map_Kd":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { material.DiffuseColorMap = material.ImageToMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "map_Ks":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { material.SpecularColorMap = material.ImageToMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "map_d":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { material.DissolveMap = material.ImageToMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "map_Ns":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { material.SpecularHighlightsMap = material.ImageToMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "map_Bump":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { material.NormalsMap = material.ImageToNormalsMap(bmp); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                                case "refl":
                                    bmp = new Bitmap($"{Path.GetDirectoryName(path)}\\{items.GetRange(1, items.Count - 1).Aggregate((i, j) => i + " " + j).Replace(',', '.')}");
                                    task = new Task(() => { var map = material.ImageToMap(bmp);  materials.ForEach(m => m.ReflectionMap = map); });
                                    task.Start();
                                    TaskList.Add(task);
                                    break;
                            }
                }
            }
            return materials;
        }
    }
}
