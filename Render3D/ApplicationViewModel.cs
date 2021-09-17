﻿using Render3D.Models;
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

        public Vector3 cameraRotation;
        public Vector3 lightDirection;

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

        public Vector3 CameraRotation { get { return cameraRotation; } set { cameraRotation = value; OnPropertyChanged("CameraRotation"); } }
        public Vector3 CameraPosition { get { return camera.Position; } set { camera.Position = value; OnPropertyChanged("CameraPosition"); } }
        public Vector3 CameraForward { get { return camera.Forward; } set { OnPropertyChanged("CameraForward"); } }
        public Camera Camera { get { return camera; } set { camera = value; OnPropertyChanged("Camera"); } }
        public Vector3 LightDirection { get { return lightDirection; } set { lightDirection = value; OnPropertyChanged("LightDirection"); } }

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

            CameraRotation = Vector3.Zero;
            LightDirection = -Vector3.UnitZ;

            camera = new Camera()
            {
                ZNear = 0.1f,
                ZFar = 200f,
                FOV = MathF.PI / 3,
                Up = new Vector3(0,1,0),
                Position = new Vector3(0, 1, 0),
                Target = new Vector3(0, 1, 0) + Vector3.UnitZ,
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
