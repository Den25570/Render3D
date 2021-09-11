using Microsoft.Win32;
using Render3D.MatrixTransformation;
using Render3D.Model;
using Render3D.Parser;
using Render3D.Render;
using System;
using System.Collections.Generic;
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

        private ObjectModel objectModel;
        private ApplicationViewModel dataContext;

        private Point? initialPosition;

        public MainWindow()
        {
            _parser = new OBJParser();
            DataContext = new ApplicationViewModel();
            dataContext = DataContext as ApplicationViewModel;
            _renderer = new BasicRenderer(dataContext.Camera);

            InitializeComponent();
        }

        private void RenderModel()
        {
            if (objectModel == null)
                return;

            float width = (float)main_canvas.ActualWidth;
            float height = (float)main_canvas.ActualHeight;

            if (!_renderer.HasBitmap)
                _renderer.CreateBitmap(main_canvas, (int)main_canvas.ActualWidth, (int)main_canvas.ActualHeight);

            //Model coodinates -> perspective coodinates
            var modelMatrix = MatrixTransformations.GetTransformationMatrix(
                new Vector3(dataContext.XScale / 100F, dataContext.YScale / 100F, dataContext.ZScale / 100F), 
                new Quaternion((MathF.PI / 180) * dataContext.XRotation, (MathF.PI / 180) * dataContext.YRotation, (MathF.PI / 180) * dataContext.ZRotation, 0), 
                new Vector3(dataContext.XTranslation, dataContext.YTranslation, dataContext.ZTranslation));
            var viewMatrix = MatrixTransformations.GetTransformationMatrixByCamera(dataContext.CameraPosition, dataContext.CameraTarget, Vector3.UnitY);
            var projectionMatrix = MatrixTransformations.GetTransformationMatrixPerspectiveProjection(dataContext.Camera.FOV, dataContext.Camera.ZNear, dataContext.Camera.ZFar, width / height);
            var viewportMatrix = MatrixTransformations.GetTransformationMatrixViewport(width, height, 0, 0);
            var finalMatrix = MatrixTransformations.GetFinalMatrix(viewportMatrix, projectionMatrix, viewMatrix, modelMatrix);
            var transformedModel = objectModel.TransformModel( modelMatrix * viewMatrix * projectionMatrix);

            //Render
            _renderer.RenderModel(transformedModel);
        }

        private void OpenItem_Click(object sender, RoutedEventArgs e)
        {

            OpenFileDialog openFileDlg = new OpenFileDialog();
            if (openFileDlg.ShowDialog() == true)
            {
                var fileName = openFileDlg.FileName;
                var model = _parser.Parse(fileName);
                objectModel = model;
                RenderModel();
            }
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TextBox_ModelTransformChanged(object sender, TextChangedEventArgs e)
        {
            if (objectModel != null)
            {
                RenderModel();
            }
        }

        private void main_canvas_KeyDown(object sender, KeyEventArgs e)
        {
            Vector3 direction = Vector3.Zero;
            Vector3 rotationDirection = Vector3.Zero;
            Matrix4x4 rotationMatrix = MatrixTransformations.GetRotationMatrix(new Quaternion((MathF.PI / 180) * -dataContext.XRotation, (MathF.PI / 180) * -dataContext.YRotation, (MathF.PI / 180) * -dataContext.ZRotation, 0));
            Vector3 forward = Vector3.Transform(dataContext.Camera.Forward, rotationMatrix);
            if (e.Key == Key.W)
            {
                direction += forward * dataContext.Camera.Speed;
            }
            if (e.Key == Key.S)
            {
                direction -= forward * dataContext.Camera.Speed;
            }

            Quaternion rotationDirection = Quaternion.Identity;
            RenderModel();
            if (e.Key == Key.Left)
            {
                rotationDirection.Y += dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Right)
            {
                rotationDirection.Y -= dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Up)
            {
                rotationDirection.X += dataContext.Camera.RotationSpeed * 10f;
            }
            if (e.Key == Key.Down)
            {
                rotationDirection.X -= dataContext.Camera.RotationSpeed * 10f;
            }
            rotationDirection.Y += dataContext.Camera.RotationSpeed * (float)xDir * 20;
            rotationDirection.X -= dataContext.Camera.RotationSpeed * (float)yDir * 20;

            Vector3 forward = dataContext.Camera.Forward;
            Quaternion quaternion = MatrixTransformations.GetRotationWorldAngles(forward, rotationDirection);
            dataContext.XRotation += quaternion.X;
            dataContext.YRotation += quaternion.Y;
            dataContext.ZRotation += quaternion.Z;

            dataContext.XTranslation += direction.X;
            dataContext.YTranslation += direction.Y;
            dataContext.ZTranslation += direction.Z;
            RenderModel();
        }

        private void main_canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            double width = (float)main_canvas.ActualWidth;
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
            }
        }

        private void main_canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed || Mouse.RightButton == MouseButtonState.Pressed)
            {
                initialPosition = Mouse.GetPosition(main_canvas);
            }
        }

        private void main_canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released || Mouse.RightButton == MouseButtonState.Released)
            {
                double width = (float)main_canvas.ActualWidth;
                double height = (float)main_canvas.ActualHeight;
                initialPosition = null;
            }
        }
    }
}
