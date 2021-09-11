using Render3D.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Render3D.Parser
{
    public interface IParser
    {
        public ObjectModel Parse(string path);
    }
}
