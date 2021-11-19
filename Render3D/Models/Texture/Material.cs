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
        public Vector3 AmbientColor { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public Vector3 SpecularColor { get; set; }

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
        public Vector3[,] AmbientColorMap { get; set; }
        public Vector3[,] DiffuseColorMap { get; set; }
        public Vector3[,] SpecularColorMap { get; set; }
        public Vector3[,] SpecularHighlightsMap { get; set; }
        public Vector3[,] DissolveMap { get; set; }
        public Vector3[,] NormalsMap { get; set; }
        public Vector3[,] ReflectionMap { get; set; }

        // reflection map
        // None yet

        public Vector3[,] ImageToNormalsMap(Bitmap img)
        {
            var map = new Vector3[img.Width, img.Height];
            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                {
                    var pixel = img.GetPixel(i, j);
                    map[i, j] = new Vector3(pixel.R / 255.0f, pixel.G / 255.0f, pixel.B / 255.0f) * 2 - Vector3.One;
                }
            return map;
        }

        public Vector3[,] ImageToMap(Bitmap img)
        {
            var map = new Vector3[img.Width,img.Height];
            for (int i = 0; i < img.Width; i++)
                for (int j = 0; j < img.Height; j++)
                {
                    var pixel = img.GetPixel(i, j);
                    map[i, j] = new Vector3(pixel.R / 255.0f, pixel.G / 255.0f, pixel.B / 255.0f);
                }
            return map;
        }

        public Vector3 GetAmbientColor(float x, float y)
        {
            if (AmbientColorMap != null)
            {
                var mapSize = AmbientColorMap.GetLength(1) - 1;
                return AmbientColorMap[(int)(x * mapSize), (int)(y * mapSize)];
            }
            return AmbientColor;
        }

        public Vector3 GetDiffuseColor(float x, float y)
        {
            if (DiffuseColorMap != null)
            {
                var mapSize = DiffuseColorMap.GetLength(1) - 1;
                return DiffuseColorMap[(int)(x * mapSize), (int)(y * mapSize)];
            }
            return DiffuseColor;
        }

        public Vector3 GetSpecularColor(float x, float y)
        {
            if (SpecularColorMap != null)
            {
                var mapSize = SpecularColorMap.GetLength(1) - 1;
                return SpecularColorMap[(int)(x * mapSize), (int)(y * mapSize)];
            }
            return SpecularColor;

        }

        public float GetSpecularHighlight(float x, float y)
        {
            if (SpecularHighlightsMap != null)
            {
                var mapSize = SpecularHighlightsMap.GetLength(1) - 1;
                return SpecularHighlightsMap[(int)(x * mapSize), (int)(y * mapSize)].X / 255.0f;
            }
            return SpecularHighlights;
        }

        public Vector3? GetNormal(float x, float y)
        {
            if (NormalsMap != null)
            {
                var mapSize = NormalsMap.GetLength(1) - 1;
                return NormalsMap[(int)(x * mapSize), (int)(y * mapSize)];
            }
            return null;
        }

        public Vector3? GetReflection(float x, float y)
        {
            if (ReflectionMap != null)
            {
                var mapSizeX = ReflectionMap.GetLength(0) - 1;
                var mapSizeY = ReflectionMap.GetLength(1) - 1;
                return ReflectionMap[(int)(x * mapSizeX), (int)(y * mapSizeY)];
            }
            return null;
        }
    }
}
