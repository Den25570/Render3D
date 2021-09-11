using Render3D.Models;
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
        private float xTranslation;
        private float yTranslation;
        private float zTranslation;

        private float xScale;
        private float yScale;
        private float zScale;

        private float xRotation;
        private float yRotation;
        private float zRotation;

        private Camera camera;

        public float XTranslation { get { return xTranslation; } set { xTranslation = value; OnPropertyChanged("XTranslation"); } }
        public float YTranslation { get { return yTranslation; } set { yTranslation = value; OnPropertyChanged("YTranslation"); } }
        public float ZTranslation { get { return zTranslation; } set { zTranslation = value; OnPropertyChanged("ZTranslation"); } }

        public float XScale { get { return xScale; } set { xScale = value; OnPropertyChanged("XScale"); } }
        public float YScale { get { return yScale; } set { yScale = value; OnPropertyChanged("YScale"); } }
        public float ZScale { get { return zScale; } set { zScale = value; OnPropertyChanged("ZScale"); } }

        public float XRotation { get { return xRotation; } set { xRotation = value; OnPropertyChanged("XRotation"); } }
        public float YRotation { get { return yRotation; } set { yRotation = value; OnPropertyChanged("YRotation"); } }
        public float ZRotation { get { return zRotation; } set { zRotation = value; OnPropertyChanged("ZRotation"); } }

        public Vector3 CameraPosition { get { return camera.Position; } set { camera.Position = value; OnPropertyChanged("CameraPosition"); } }
        public Vector3 CameraTarget { get { return camera.Target; } set { camera.Target = value; OnPropertyChanged("CameraTarget"); } }
        public Camera Camera { get { return camera; } set { camera = value; OnPropertyChanged("Camera"); } }

        public ApplicationViewModel()
        {
            XTranslation = 0;
            YTranslation = 1;
            ZTranslation = 5;

            XRotation = 0;
            YRotation = 0;
            ZRotation = 0;

            XScale = 100;
            YScale = 100;
            ZScale = 100;

            camera = new Camera()
            {
                ZNear = 0.1f,
                ZFar = 200f,
                FOV = MathF.PI / 4,
                Speed = 0.5f,
                RotationSpeed = 0.3f,
                MouseSpeed = 1f,
                Position = new Vector3(0, 0, 0),
                Target = new Vector3(0, 1, 5),
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
