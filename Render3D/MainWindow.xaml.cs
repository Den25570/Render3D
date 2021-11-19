﻿using Microsoft.Win32;
using Render3D.Math;
using Render3D.Models;
using Render3D.Parser;
using Render3D.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Render3D
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Services
        private IParser _parser;
        private IParser _materialParser;
        private Dictionary<RenderMode, IRenderer> _renderers;
        private IRenderer _renderer;

        // Data
        private Model model;
        private Scene scene;
        private ApplicationViewModel dataContext;

        // Control params
        private Point? initialPosition;
        public float Speed = 0.5f;
        public float RotationSpeed = 0.05f;
        public float MouseSpeed = 0.1f;

        public MainWindow()
        {
            _materialParser = new MaterialParser();
            _parser = new OBJParser(_materialParser);

            DataContext = new ApplicationViewModel();
            dataContext = DataContext as ApplicationViewModel;

            scene = new Scene()
            {
                Lights = new Vector4[] { dataContext.lightPosition },
                LightsColors = new Vector3[] { Vector3.One },
                MainCamera = dataContext.Camera,
                BackgroundLightIntensity = 0.1f,
            };

            _renderers = new Dictionary<RenderMode, IRenderer>()
            {
                {RenderMode.Texture, new TextureRenderer() },
                {RenderMode.Phong, new PhongRenderer() },
                {RenderMode.SimpleTriangle, new FlatRenderer() },
                {RenderMode.Wireframe, new WireframeRenderer() },
            };
            _renderer = _renderers[dataContext.RenderMode];

            InitializeComponent();
        }

        private void RenderModel()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            
            if (model != null)
            {
                // Matrices
                var viewModel = new Model(model);
                var modelMatrix = Math3D.GetTransformationMatrix(
                    new Vector3(dataContext.XScale / 100F, dataContext.YScale / 100F, dataContext.ZScale / 100F),
                    new Vector3((MathF.PI / 180) * dataContext.XRotation, (MathF.PI / 180) * dataContext.YRotation, (MathF.PI / 180) * dataContext.ZRotation),
                    new Vector3(dataContext.XTranslation, dataContext.YTranslation, dataContext.ZTranslation));
                var viewMatrix = Math3D.GetViewMatrix(dataContext.Camera.Position, dataContext.Camera.Rotation);
                var projectionMatrix = Math3D.GetPerspectiveProjectionMatrix(dataContext.Camera.FOV, dataContext.Camera.ZNear, dataContext.Camera.ZFar, (float)main_canvas.ActualWidth / (float)main_canvas.ActualHeight);
                var viewportMatrix = Math3D.GetViewportMatrix((float)main_canvas.ActualWidth, (float)main_canvas.ActualHeight, 0, 0);

                // Transformations
                viewModel.TransformModel(modelMatrix, true); // Model -> World  
                viewModel.RemoveHiddenFaces(dataContext.Camera.Position); // Remove hidden faces
                viewModel.CalculateColor(scene); // Model colors
                viewModel.TransformModel(viewMatrix); // World -> View
                viewModel.ClipTriangles(new Vector3(0, 0, dataContext.Camera.ZNear), new Vector3(0, 0, 1)); // view clip Z near
                viewModel.TransformModel(projectionMatrix * viewportMatrix); // 3D -> 2D projection | 2D projection -> viewport projection
                viewModel.ClipTriangles(new Vector3(0, 0, 0), Vector3.UnitY); // viewport clip Y
                viewModel.ClipTriangles(new Vector3(0, (float)main_canvas.ActualHeight - 1, 0), -Vector3.UnitY); // viewport clip -Y
                viewModel.ClipTriangles(new Vector3(0, 0, 0), Vector3.UnitX); // viewport clip X
                viewModel.ClipTriangles(new Vector3((float)main_canvas.ActualWidth - 1, 0, 0), -Vector3.UnitX); // viewport clip -X

                // Transformation to world view with all clipped triangles
                Matrix4x4.Invert(viewMatrix, out var invView);
                Matrix4x4.Invert(projectionMatrix, out var invProj);
                Matrix4x4.Invert(viewportMatrix, out var invViewport);
                var worldModel = new Model(viewModel);
                worldModel.TransformModel(invViewport * invProj * invView);

                // Shadow casting
                for(int i = 0; i < scene.Lights.Length; i++)
                {
                    var lightSpaceMatrix = Math3D.GetLightViewMatrix(scene.Lights[i].ToVector3(), Vector3.Zero, dataContext.Camera.ZNear, dataContext.Camera.ZFar);
                }

                // Render
                _renderer.RenderModel(viewModel, worldModel, scene);
            }
            stopwatch.Stop();
            dataContext.FPS = stopwatch.ElapsedMilliseconds;
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Filter = "obj files (*.obj)|*.obj";
            if (openFileDlg.ShowDialog() == true)
            {
                // Load model
                var fileName = openFileDlg.FileName;
                var loadedModel = (ObjectModel)_parser.Parse(fileName);

                // Preprocess model
                loadedModel.CalculateNormals();
                model = new Model(loadedModel);

                // Render model
                RenderModel();
            }
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_ModelTransformChanged(object sender, TextChangedEventArgs e)
        {
            RenderModel();
        }

        private void main_canvas_KeyDown(object sender, KeyEventArgs e)
        {
            var rot = Quaternion.CreateFromYawPitchRoll(dataContext.Camera.Rotation.X, dataContext.Camera.Rotation.Y, dataContext.Camera.Rotation.Z);
            Vector3 forward = Vector3.Transform(Vector3.UnitZ, rot) * 0.1f;

            //Move
            if (e.Key == Key.W)
            {
                dataContext.Camera.Position += forward;
            }
            if (e.Key == Key.S)
            {
                dataContext.Camera.Position -= forward;
            }

            //Rotate
            if (e.Key == Key.Up)
            {
                dataContext.Camera.Rotate(new Vector3(0, 0.05f, 0));
            }
            if (e.Key == Key.Down)
            {
                dataContext.Camera.Rotate(new Vector3(0, -0.05f, 0));
            }
            if (e.Key == Key.Left)
            {
                dataContext.Camera.Rotate(new Vector3(0.05f, 0, 0));
            }
            if (e.Key == Key.Right)
            {
                dataContext.Camera.Rotate(new Vector3(-0.05f, 0, 0));
            }

            //Scale model
            if (e.Key == Key.Add)
            {
                dataContext.XScale += 5;
                dataContext.YScale += 5;
                dataContext.ZScale += 5;
            }
            if (e.Key == Key.Subtract)
            {
                dataContext.XScale -= 5;
                dataContext.YScale -= 5;
                dataContext.ZScale -= 5;
            }

            // Utils
            if (e.Key == Key.F1)
            {
                dataContext.RenderMode = RenderMode.Texture;
                _renderer = _renderers[dataContext.RenderMode];
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            }
            if (e.Key == Key.F2)
            {
                dataContext.RenderMode = RenderMode.Phong;
                _renderer = _renderers[dataContext.RenderMode];
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            }
            if (e.Key == Key.F3)
            {
                dataContext.RenderMode = RenderMode.SimpleTriangle;
                _renderer = _renderers[dataContext.RenderMode];
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            }
            if (e.Key == Key.F4)
            {
                dataContext.RenderMode = RenderMode.Wireframe;
                _renderer = _renderers[dataContext.RenderMode];
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            }

            RenderModel();
        }

        private void main_canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Point position = Mouse.GetPosition(main_canvas);
                initialPosition = initialPosition ?? position;
                double xDir = (position.X - initialPosition.Value.X) / (main_canvas.ActualWidth / 2.0);
                double yDir = -(position.Y - initialPosition.Value.Y) / (main_canvas.ActualHeight / 2.0);
                dataContext.Camera.Rotate(new Vector3((float)xDir, (float)yDir, 0));

                RenderModel();

                initialPosition = position;
            }
        }

        private void main_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                initialPosition = Mouse.GetPosition(main_canvas);
            }
        }

        private void main_canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                initialPosition = null;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            if(model != null)
                RenderModel();
        }
    }
}
