using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
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
            Material material = new Material();
            using (StreamReader reader = File.OpenText(path))
            {
                for (string stringLine; (stringLine = reader.ReadLine()) != null;)
                {
                    List<string> items = stringLine.Replace('.', ',').Split(' ').ToList();
                    items.RemoveAll(s => s == "");

                    switch (items[0])
                    {

                    }
                }
            }
            return material;
        }
    }
}
