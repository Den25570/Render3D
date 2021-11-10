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
            Material material = null;

            if (File.Exists(path))
            using (StreamReader reader = File.OpenText(path))
            {
                for (string stringLine; (stringLine = reader.ReadLine()) != null;)
                {
                    List<string> items = stringLine.Replace('.', ',').Split(' ').ToList();
                    items.RemoveAll(s => s == "");

                    if (items.Count > 0)
                            switch (items[0])
                            {
                                // Material color and illumination statements:
                                case "newmtl":
                                    material = new Material();
                                    materials.Add(material);
                                    material.Name = items[1];
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
                                case "Ns ":
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
                                    material.AmbientColorMap = Image.FromFile(items[1]);
                                    break;
                                case "map_Kd":
                                    material.DiffuseColorMap = Image.FromFile(items[1]);
                                    break;
                                case "map_Ks":
                                    material.SpecularColorMap = Image.FromFile(items[1]);
                                    break;
                                case "map_d":
                                    material.DissolveMap = Image.FromFile(items[1]);
                                    break;
                                case "map_Ns":
                                    material.SpecularHighlightsMap = Image.FromFile(items[1]);
                                    break;
                            }
                }
            }
            return materials;
        }
    }
}
