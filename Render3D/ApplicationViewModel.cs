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

        private float xScale;
        private float yScale;
        private float zScale;

        private float xRotation;
        private float yRotation;
        private float zRotation;

        public Vector4 lightPosition;

        public RenderMode renderMode;

        private Camera camera;

        public float FPS { get { return timePerRender; } set { timePerRender = value; OnPropertyChanged("FPS"); } }
        public float XTranslation { get { return xTranslation; } set { xTranslation = value; OnPropertyChanged("XTranslation"); } }
        public float YTranslation { get { return yTranslation; } set { yTranslation = value; OnPropertyChanged("YTranslation"); } }
        public float ZTranslation { get { return zTranslation; } set { zTranslation = value; OnPropertyChanged("ZTranslation"); } }

        public float XScale { get { return xScale; } set { xScale = value; OnPropertyChanged("XScale"); } }
        public float YScale { get { return yScale; } set { yScale = value; OnPropertyChanged("YScale"); } }
        public float ZScale { get { return zScale; } set { zScale = value; OnPropertyChanged("ZScale"); } }

        public float XRotation { get { return xRotation; } set { xRotation = value; OnPropertyChanged("XRotation"); } }
        public float YRotation { get { return yRotation; } set { yRotation = value; OnPropertyChanged("YRotation"); } }
        public float ZRotation { get { return zRotation; } set { zRotation = value; OnPropertyChanged("ZRotation"); } }

        public Camera Camera { get { return camera; } set { camera = value; OnPropertyChanged("Camera"); } }
        public RenderMode RenderMode { get { return renderMode; } set { renderMode = value; OnPropertyChanged("RenderMode"); } }
        public Vector4 LightPosition { get { return lightPosition; } set { lightPosition = value; OnPropertyChanged("LightPosition"); } }

        public ApplicationViewModel()
        {
            XTranslation = 0;
            YTranslation = 0;
            ZTranslation = 4;

            XRotation = 0;
            YRotation = 0;
            ZRotation = 0;

            XScale = 100;
            YScale = 100;
            ZScale = 100;

            LightPosition = Vector4.UnitY * 10;
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
