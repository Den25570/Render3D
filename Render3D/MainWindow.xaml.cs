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
            dataContext.Camera.Rotate(dataContext.CameraRotation);
            dataContext.CameraForward = Vector3.Zero;
            var viewMatrix = Math.Math3D.GetLookAtMatrix(dataContext.Camera.Position, dataContext.Camera.Target, Vector3.UnitY);
            var InverseViewMatrix = Math.Math3D.GetInverseMatrix(viewMatrix);
            var projectionMatrix = Math.Math3D.GetPerspectiveProjectionMatrix(dataContext.Camera.FOV, dataContext.Camera.ZNear, dataContext.Camera.ZFar, width / height);
            var viewportMatrix = Math.Math3D.GetViewportMatrix(width, height, 0, 0);

            // Model coodinates -> perspective coodinates
            stopwatch.Stop();
            var a0 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();
            var transformedModel = model.TransformModel(modelMatrix);
            stopwatch.Stop();
            var a1 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();
            // RemoveHiddenFaces
            transformedModel = transformedModel.RemoveHiddenFaces(dataContext.Camera.Position);
            stopwatch.Stop();
            var a2 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();
            // Camera
            transformedModel = transformedModel.TransformModel(InverseViewMatrix);
            stopwatch.Stop();
            var a3 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();
            // Clip triangles to camera
            transformedModel = transformedModel.ClipTriangles(
                new Vector3(0, 0, dataContext.Camera.ZNear),
                new Vector3(0, 0, 1));
            // 3D -> 2D
            stopwatch.Stop();
            var a4 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();
            transformedModel = transformedModel.TransformModel(projectionMatrix);
            stopwatch.Stop();
            var a5 = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();

            // Convert to current viewport
            transformedModel = transformedModel.TransformModel(viewportMatrix);

            stopwatch.Stop();
            var a = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();

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

            stopwatch.Stop();
            var b = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();

            //Render
            _renderer.RenderModel(transformedModel, dataContext.lightDirection);

            stopwatch.Stop();
            var c = stopwatch.ElapsedMilliseconds;
            stopwatch.Start();

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
            

            Quaternion rotationDirection = Quaternion.Identity;
            Matrix4x4 rotationMatrix = Math.Math3D.GetRotationMatrix(new Vector3((MathF.PI / 180) * -dataContext.XRotation, (MathF.PI / 180) * -dataContext.YRotation, (MathF.PI / 180) * -dataContext.ZRotation));

            //Vector3 forward = Vector3.Transform(dataContext.Camera.Forward, rotationMatrix);

            Vector3 direction = Vector3.Zero;
            if (e.Key == Key.Up)
            {
                direction.Y += Speed;
            }
            if (e.Key == Key.Down)
            {
                direction.Y -= Speed;
            }
            if (e.Key == Key.Left)
            {
                direction.X -= Speed;
            }
            if (e.Key == Key.Right)
            {
                direction.X +=  Speed;
            }

            Vector3 forward = dataContext.Camera.Forward * 0.1f;
            forward.X = -forward.X;
            forward.Y = -forward.Y;
            if (e.Key == Key.W)
            {
                dataContext.Camera.Position -= forward;
            }
            if (e.Key == Key.S)
            {
                dataContext.Camera.Position += forward;
            }

            Vector3 rotation = Vector3.Zero;
            if (e.Key == Key.A)
            {
                rotation.Y -= RotationSpeed;
            }
            if (e.Key == Key.D)
            {
                rotation.Y += RotationSpeed;
            }
            dataContext.CameraRotation += rotation;
            /*
            if (e.Key == Key.Left)
            {
                rotationDirection.Y -= dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Right)
            {
                rotationDirection.Y += dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Up)
            {
                rotationDirection.X += dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Down)
            {
                rotationDirection.X -= dataContext.Camera.RotationSpeed * 10f;
            }*/

            /* dataContext.Camera.Position += direction;
             dataContext.Camera.Target += direction;*/

            /*Quaternion quaternion = MatrixTransformations.GetRotationWorldAngles(forward, rotationDirection * (MathF.PI / 180)) * (1 / (MathF.PI / 180));
            dataContext.XRotation += quaternion.X;
            dataContext.YRotation += quaternion.Y;
            dataContext.ZRotation += quaternion.Z;*/

            dataContext.Camera.Position += direction;
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
