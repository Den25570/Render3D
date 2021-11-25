using Render3D.Models;
using Render3D.Render;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Render3D
{
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        private float timePerRender;

        private float xTranslation;
        private float yTranslation;
        private float zTranslation;
        private float scale;
        private float xRotation;
        private float yRotation;
        private float zRotation;

        private Vector3 lightColor;
        public Vector4 lightPosition;
        public RenderMode renderMode;
        private Camera camera;

        public bool IsWorldChanged { get; set; }
        public float FPS { get { return timePerRender; } set { timePerRender = value; OnPropertyChanged("FPS"); } }
        public float XTranslation { get { return xTranslation; } set { IsWorldChanged = true; xTranslation = value; OnPropertyChanged("XTranslation"); } }
        public float YTranslation { get { return yTranslation; } set { IsWorldChanged = true; yTranslation = value; OnPropertyChanged("YTranslation"); } }
        public float ZTranslation { get { return zTranslation; } set { IsWorldChanged = true; zTranslation = value; OnPropertyChanged("ZTranslation"); } }

        public float Scale { get { return scale; } set { IsWorldChanged = true; scale = value; OnPropertyChanged("Scale"); } }

        public float XRotation { get { return xRotation; } set { IsWorldChanged = true; xRotation = value; OnPropertyChanged("XRotation"); } }
        public float YRotation { get { return yRotation; } set { IsWorldChanged = true; yRotation = value; OnPropertyChanged("YRotation"); } }
        public float ZRotation { get { return zRotation; } set { IsWorldChanged = true; zRotation = value; OnPropertyChanged("ZRotation"); } }

        public float RLightColor { get { return lightColor.X * 255; } set { IsWorldChanged = true; lightColor = new Vector3(value / 255, lightColor.Y, lightColor.Z); OnPropertyChanged("RLightColor"); } }
        public float GLightColor { get { return lightColor.Y * 255; } set { IsWorldChanged = true; lightColor = new Vector3(lightColor.X, value / 255, lightColor.Z); OnPropertyChanged("GLightColor"); } }
        public float BLightColor { get { return lightColor.Z * 255; } set { IsWorldChanged = true; lightColor = new Vector3(lightColor.X, lightColor.Y, value / 255); OnPropertyChanged("BLightColor"); } }
        public Vector3 LightColor { get { return lightColor; } set { IsWorldChanged = true; lightColor = value; OnPropertyChanged("BLightColor"); } }

        public Camera Camera { get { return camera; } set { camera = value; OnPropertyChanged("Camera"); } }
        public RenderMode RenderMode { get { return renderMode; } set { renderMode = value; OnPropertyChanged("RenderMode"); } }
        public Vector4 LightPosition { get { return lightPosition; } set { lightPosition = value; OnPropertyChanged("LightPosition"); } }

        public ApplicationViewModel()
        {
            XTranslation = 0;
            YTranslation = 0;
            ZTranslation = 4;

            XRotation = 0;
            YRotation = 180;
            ZRotation = 0;

            Scale = 100;

            LightPosition = new Vector4(1, 10, -15, 1);
            LightColor = Vector3.One;
            RenderMode = RenderMode.Texture;

            camera = new Camera()
            {
                Position = new Vector3(0, 5, -2),
                Rotation = Vector3.Zero,
            }; 
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
