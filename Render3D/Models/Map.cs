using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Models
{
    public class Map<T>
    {
        public T this[int i]
        {
            get { return A[i]; }
            set { A[i] = value; }
        }
        public T this[int i, int j]
        {
            get { return A[i * Width + j]; }
            set { A[i * Width + j] = value; }
        }
        public T this[float x, float y]
        {
            get { return A[(int)(x * Width) * Width + (int)(y * Height)]; }
        }
        public int Width { get; set; }
        public int Height { get; set; }
        public T[] A { get; set; }
    }
}
