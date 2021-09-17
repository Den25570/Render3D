using Microsoft.Win32;
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
            dataContext.CameraForward = Vector3.Zero;
            var viewMatrix = Math.Math3D.GetLookAtMatrix(dataContext.Camera.Position, dataContext.Camera.Target, dataContext.Camera.Up, dataContext.Camera.Right);
            viewMatrix = Math.Math3D.GetInverseMatrix(viewMatrix);
            var projectionMatrix = Math.Math3D.GetPerspectiveProjectionMatrix(dataContext.Camera.FOV, dataContext.Camera.ZNear, dataContext.Camera.ZFar, width / height);
            var viewportMatrix = Math.Math3D.GetViewportMatrix(width, height, 0, 0);

            // Model coodinates -> perspective coodinates
            var transformedModel = model.TransformModel(modelMatrix, true);
            // RemoveHiddenFaces
            transformedModel = transformedModel.RemoveHiddenFaces(dataContext.Camera.Position);
            // Camera
            transformedModel = transformedModel.TransformModel(viewMatrix);
            // Clip triangles to camera
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, 0, dataContext.Camera.ZNear),
                new Vector3(0, 0, 1));
            // 3D -> 2D
            transformedModel = transformedModel.TransformModel(projectionMatrix);

            // Convert to current viewport
            transformedModel = transformedModel.TransformModel(viewportMatrix);

            //Clip triangles to screenbox
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
            _renderer.RenderModel(transformedModel, dataContext.lightDirection);

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
            Vector3 forward = dataContext.Camera.Forward * 0.1f;
            forward.X = -forward.X;
            forward.Y = -forward.Y;

            Vector3 direction = Vector3.Zero;
            Vector3 cameraRotation = Vector3.Zero;
            if (e.Key == Key.W)
            {
                direction += forward;
            }
            if (e.Key == Key.S)
            {
                direction -= forward;
            }
            if (e.Key == Key.A)
            {
                cameraRotation.Y -= RotationSpeed;
            }
            if (e.Key == Key.D)
            {
                cameraRotation.Y += RotationSpeed;
            }
            if (e.Key == Key.Q)
            {
                cameraRotation.X -= RotationSpeed;
            }
            if (e.Key == Key.E)
            {
                cameraRotation.X += RotationSpeed;
            }

            if (e.Key == Key.Up)
            {
                dataContext.XRotation += RotationSpeed * 15;
            }
            if (e.Key == Key.Down)
            {
                dataContext.XRotation -= RotationSpeed * 15;
            }
            if (e.Key == Key.Left)
            {
                dataContext.YRotation -= RotationSpeed * 15;
            }
            if (e.Key == Key.Right)
            {
                dataContext.YRotation += RotationSpeed * 15;
            }

            dataContext.Camera.Move(direction);
            dataContext.Camera.Rotate(cameraRotation);
            
            RenderModel();
        }

        private void main_canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            /*double width = (float)main_canvas.ActualWidth;
            double height = (float)main_canvas.ActualHeight;
            Point position = Mouse.GetPosition(main_canvas);
            initialPosition = initialPosition ?? position;
            double xDir = (position.X - initialPosition.Value.X) / (width / 2);
            double yDir = (position.Y - initialPosition.Value.Y) / (height / 2);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Matrix4x4 rotationMatrix = MatrixTransformations.GetRotationMatrix(new Quaternion((MathF.PI / 180) * -dataContext.XRotation, (MathF.PI / 180) * -dataContext.YRotation, (MathF.PI / 180) * -dataContext.ZRotation, 0));
                Vector3 forward = Vector3.Transform(dataContext.Camera.Forward, rotationMatrix);
                Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
                Vector3 up = Vector3.Cross(forward, right);

                dataContext.XTranslation -= dataContext.Camera.Speed * (float)xDir * 5;
                dataContext.YTranslation -= dataContext.Camera.Speed * (float)yDir * 5;

                RenderModel();

                initialPosition = position;
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                Quaternion rotationDirection = Quaternion.Identity;
                rotationDirection.Y += dataContext.Camera.RotationSpeed * (float)xDir * 20;
                rotationDirection.X -= dataContext.Camera.RotationSpeed * (float)yDir * 20;

                Vector3 forward = dataContext.Camera.Forward;
                Quaternion quaternion = MatrixTransformations.GetRotationWorldAngles(forward, rotationDirection);
                dataContext.XRotation += quaternion.X;
                dataContext.YRotation += quaternion.Y;
                dataContext.ZRotation += quaternion.Z;
                RenderModel();
                initialPosition = position;
            }*/
        }

        private void main_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            /*if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed)
            {
                initialPosition = Mouse.GetPosition(main_canvas);
            }*/
        }

        private void main_canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            /*if (Mouse.LeftButton == MouseButtonState.Released || Mouse.RightButton == MouseButtonState.Released)
            {
                double width = (float)main_canvas.ActualWidth;
                double height = (float)main_canvas.ActualHeight;
                initialPosition = null;
            }*/
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);
            if(model != null)
                RenderModel();
        }
    }
}
