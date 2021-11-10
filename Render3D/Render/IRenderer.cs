﻿using Render3D.Models;
using Render3D.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Render3D.Render
{
    public interface IRenderer
    {
        public bool HasBitmap { get;}
        public void CreateBitmap(Canvas canvas, int width, int height);

        void RenderModel(Model model, Material material, Scene world);
    }
}
