using Render3D.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models.Texture
{
    public class Material
    {
        public string Name { get; set; }

        // Material color & illumination
        public Vector3 AmbientColor { get; set; } = Vector3.One;
        public Vector3 DiffuseColor { get; set; } = Vector3.One;
        public Vector3 SpecularColor { get; set; } = Vector3.One;

        /*"Ns "A high exponent 
        results in a tight, concentrated highlight.  Ns values normally range 
        from 0 to 1000.*/
        public float SpecularHighlights { get; set; } = 60;

        /*"d" is the amount this material dissolves into the background.  A 
        factor of 1.0 is fully opaque.  This is the default when a new material 
        is created.  A factor of 0.0 is fully dissolved (completely 
        transparent).*/
        public float Dissolve { get; set; }

        /*"illum" Illumination models are mathematical equations that represent 
        various material lighting and shading effects.*/
        public float IlluminationModel { get; set; }

        /*"tf" Any light passing through the object is filtered by the transmission 
        filter, which only allows the specifiec colors to pass through.  For 
        example, Tf 0 1 0 allows all the green to pass through and filters out 
        all the red and blue.*/
        public Vector3 TransmissionFilter { get; set; }

        /*"sharpness" the sharpness of the reflections from the local reflection 
        map.  If a material does not have a local reflection map defined in its 
        material definition, sharpness will apply to the global reflection map 
        defined in PreView. 0 to 1000*/
        public float Sharpness { get; set; }

        /*"Ni" is the value for the optical density.  The values can 
        range from 0.001 to 10.  A value of 1.0 means that light does not bend 
        as it passes through an object.  Increasing the optical_density 
        increases the amount of bending.  Glass has an index of refraction of 
        about 1.5.  Values of less than 1.0 produce bizarre results and are not 
        recommended.*/
        public float OpticalDensity { get; set; }

        // texture maps
        public int[,] AmbientColorMap { get; set; }
        public int[,] DiffuseColorMap { get; set; }
        public int[,] SpecularColorMap { get; set; }
        public float[,] SpecularHighlightsMap { get; set; }
        public float[,] DissolveMap { get; set; }
        public int[,] NormalsMap { get; set; }
        public int[,] ReflectionMap { get; set; }

        public int[,] ImageToMap(Bitmap img)
        {
            var map = new int[img.Width, img.Height];
            using (var snoop = new BmpPixelSnoop(img))
            {
                for (int i = 0; i < snoop.Width; i++)
                    for (int j = 0; j < snoop.Height; j++)
                    {
                        var pixel = snoop.GetPixel(i, j);
                        map[i, j] = pixel.ToArgb();
                    }
            }
            return map;
        }

        public float[,] ImageToMapFloat(Bitmap img)
        {
            var map = new float[img.Width, img.Height];
            using (var snoop = new BmpPixelSnoop(img))
            {
                for (int i = 0; i < snoop.Width; i++)
                    for (int j = 0; j < snoop.Height; j++)
                    {
                        var pixel = snoop.GetPixel(i, j);
                        map[i, j] = pixel.R / 255.0f;
                    }
            }
            return map;
        }

        public Vector3 GetAmbientColor(float x, float y)
        {
            if (AmbientColorMap != null)
            {
                var mapSize = AmbientColorMap.GetLength(1) - 1;
                var value = AmbientColorMap[(int)(x * mapSize), (int)(y * mapSize)];
                return new Vector3(((value >> 16) & 0xFF) / 255.0f, ((value >> 8) & 0xFF) / 255.0f, (value & 0xFF) / 255.0f);
            }
            return AmbientColor;
        }

        public Vector3 GetDiffuseColor(float x, float y)
        {
            if (DiffuseColorMap != null)
            {
                var mapSize = DiffuseColorMap.GetLength(1) - 1;
                var value = DiffuseColorMap[(int)(x * mapSize), (int)(y * mapSize)];
                return new Vector3(((value >> 16) & 0xFF) / 255.0f, ((value >> 8) & 0xFF) / 255.0f, (value & 0xFF) / 255.0f);
            }
            return DiffuseColor;
        }

        public Vector3 GetSpecularColor(float x, float y)
        {
            if (SpecularColorMap != null)
            {
                var mapSize = SpecularColorMap.GetLength(1) - 1;
                var value = SpecularColorMap[(int)(x * mapSize), (int)(y * mapSize)];
                return new Vector3(((value >> 16) & 0xFF) / 255.0f, ((value >> 8) & 0xFF) / 255.0f, (value & 0xFF) / 255.0f);
            }
            return SpecularColor;

        }

        public float GetSpecularHighlight(float x, float y, float modif)
        {
            if (SpecularHighlightsMap != null)
            {
                var mapSize = SpecularHighlightsMap.GetLength(1) - 1;
                return SpecularHighlightsMap[(int)(x * mapSize), (int)(y * mapSize)] * SpecularHighlights * modif;
            }
            return SpecularHighlights;
        }

        public Vector3? GetNormal(float x, float y)
        {
            if (NormalsMap != null)
            {
                var mapSize = NormalsMap.GetLength(1) - 1;
                var value = NormalsMap[(int)(x * mapSize), (int)(y * mapSize)];
                return new Vector3((((value >> 16) & 0xFF) / 255.0f) * 2 - 1, (((value >> 8) & 0xFF) / 255.0f) * 2 - 1, ((value & 0xFF) / 255.0f) * 2 - 1); ;
            }
            return null;
        }

        public Vector3? GetReflection(float x, float y)
        {
            if (ReflectionMap != null)
            {
                var mapSizeX = ReflectionMap.GetLength(0) - 1;
                var mapSizeY = ReflectionMap.GetLength(1) - 1;
                var value = ReflectionMap[(int)(x * mapSizeX), (int)(y * mapSizeY)];
                return new Vector3(((value >> 16) & 0xFF) / 255.0f, ((value >> 8) & 0xFF) / 255.0f, (value & 0xFF) / 255.0f);
            }
            return null;
        }

        public float GetDissolve(float x, float y)
        {
            if (DissolveMap != null)
            {
                var mapSizeX = ReflectionMap.GetLength(0) - 1;
                var mapSizeY = ReflectionMap.GetLength(1) - 1;
                return DissolveMap[(int)(x * mapSizeX), (int)(y * mapSizeY)] * Dissolve;
            }
            return Dissolve;
        }
    }
}
