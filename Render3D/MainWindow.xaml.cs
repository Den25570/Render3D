﻿using Microsoft.Win32;
using Render3D.Math;
using Render3D.Models;
using Render3D.Parser;
using Render3D.Render;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Render3D
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IParser _parser;
        private IRenderer _renderer;

        private Model model;
        private ApplicationViewModel dataContext;

        private Point? initialPosition;
        public float Speed = 0.5f;
        public float RotationSpeed = 0.05f;
        public float MouseSpeed = 0.1f;

        public MainWindow()
        {
            _parser = new OBJParser();
            DataContext = new ApplicationViewModel();
            dataContext = DataContext as ApplicationViewModel;
            _renderer = new TriangleRenderer();

            InitializeComponent();
        }

        private void RenderModel()
        {
            if (model == null)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();

            float width = (float)main_canvas.ActualWidth;
            float height = (float)main_canvas.ActualHeight;

            if (!_renderer.HasBitmap)
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);

            var modelMatrix = Math.Math3D.GetTransformationMatrix(
                new Vector3(dataContext.XScale / 100F, dataContext.YScale / 100F, dataContext.ZScale / 100F), 
                new Vector3((MathF.PI / 180) * dataContext.XRotation, (MathF.PI / 180) * dataContext.YRotation, (MathF.PI / 180) * dataContext.ZRotation), 
                new Vector3(dataContext.XTranslation, dataContext.YTranslation, dataContext.ZTranslation));
            var viewMatrix = Math3D.GetViewMatrix(dataContext.Camera.Position, dataContext.Camera.Rotation);
            var projectionMatrix = Math.Math3D.GetPerspectiveProjectionMatrix(dataContext.Camera.FOV, dataContext.Camera.ZNear, dataContext.Camera.ZFar, width / height);
            var viewportMatrix = Math.Math3D.GetViewportMatrix(width, height, 0, 0);

            // Model -> World
            var transformedModel = model.TransformModel(modelMatrix, true);
            // Remove hidden faces
            transformedModel = transformedModel.RemoveHiddenFaces(dataContext.Camera.Position);
            // World -> View
            transformedModel = transformedModel.TransformModel(viewMatrix);
            // View -> Clip
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, 0, dataContext.Camera.ZNear),
                new Vector3(0, 0, 1));
            // 3D -> 2D
            transformedModel = transformedModel.TransformModel(projectionMatrix);
            transformedModel = transformedModel.TransformModel(viewportMatrix);
            // 2D -> CLip
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, 0, 0),
                Vector3.UnitY);
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, height - 1, 0),
                -Vector3.UnitY);
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, 0, 0),
                Vector3.UnitX);
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(width - 1, 0, 0),
                -Vector3.UnitX);

            //Render
            _renderer.RenderModel(transformedModel, dataContext.RenderMode, dataContext.lightDirection);

            stopwatch.Stop();
            dataContext.FPS = 1f / ((stopwatch.ElapsedMilliseconds > 0 ? stopwatch.ElapsedMilliseconds : 0.01f) / 1000f);
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Filter = "obj files (*.obj)|*.obj";
            if (openFileDlg.ShowDialog() == true)
            {
                // Load model
                var fileName = openFileDlg.FileName;
                var loadedModel = _parser.Parse(fileName);

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
            if (model != null)
            {
                RenderModel();
            }
        }

        private void main_canvas_KeyDown(object sender, KeyEventArgs e)
        {
            var rot = Quaternion.CreateFromYawPitchRoll(dataContext.Camera.Rotation.X, dataContext.Camera.Rotation.Y, dataContext.Camera.Rotation.Z);
            Vector3 forward = Vector3.Transform(Vector3.UnitZ, rot) * 0.1f;
            Vector3 right = Vector3.Transform(Vector3.UnitZ, rot) * 0.1f;

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
                dataContext.Camera.Rotation -= new Vector3(0, 0.05f, 0);
            }
            if (e.Key == Key.Down)
            {
                dataContext.Camera.Rotation += new Vector3(0, 0.05f, 0);
            }
            if (e.Key == Key.Left)
            {
                dataContext.Camera.Rotation += new Vector3(0.05f, 0, 0);
            }
            if (e.Key == Key.Right)
            {
                dataContext.Camera.Rotation -= new Vector3(0.05f, 0, 0);
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
                dataContext.RenderMode = RenderMode.Rasterization;
            }
            if (e.Key == Key.F2)
            {
                dataContext.RenderMode = RenderMode.Wireframe;
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
                dataContext.Camera.Rotation += new Vector3((float)xDir, (float)yDir, 0);

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