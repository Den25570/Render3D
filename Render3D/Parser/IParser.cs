using Render3D.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Parser
{
    public interface IParser
    {
        public Object Parse(string path);
    }
}
