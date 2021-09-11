using Render3D.Model;
using Render3D.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Render3D.Render
{
    public class BasicRenderer : IRenderer
    {
        private Camera _camera;
        private WriteableBitmap _bitmap;
        private int _width;
        private int _height;

        public bool HasBitmap { get => _bitmap != null; }

        public BasicRenderer(Camera camera)
        {
            _camera = camera;
        }

        public void CreateBitmap(Canvas canvas, int width, int height)
        {
            _width = width;
            _height = height;
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            Image image = new Image();
            image.Source = _bitmap;
            canvas.Children.Clear();
            canvas.Children.Add(image);
        }

        private bool isInScreenSpace(Vector4 v)
        {
            return /*v.X >= -1 && v.X <= 1 && v.Y >= -1 && v.Y <= 1 &&*/ 
                v.Z * v.W >= _camera.ZNear && v.Z * v.W <= _camera.ZFar;
        }

        public void RenderModel(ObjectModel objectModel)
        {
            _bitmap.Clear(Color.FromRgb(255, 255, 255));
            var brush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            for (int j = 0; j < objectModel.Faces.Length; j++)
            {
                var face = objectModel.Faces[j];
                for (int i = 0; i < face.Count; i++)
                {
                    int endpoint = i == 0 ? face.Count - 1 : i - 1;
                    var v1 = (objectModel.Vertices[(int)face[i].X - 1]);
                    var v2 = (objectModel.Vertices[(int)face[endpoint].X - 1]);

                    // -1 = 0, 0 = width / 2, 1 = width

                    if (isInScreenSpace(v1) && isInScreenSpace(v2))
                    {
                        _bitmap.DrawLine(
                            (int)((v1.X + 1) * _width / 2),
                            (int)((1 - (v1.Y + 1) / 2) * _height ),
                            (int)((v2.X + 1) * _width / 2),
                            (int)((1 - (v2.Y + 1) / 2) * _height),
                            brush.Color);
                    }
                }
            }

            //var bitmapImage = ConvertWriteableBitmapToBitmapImage(_bitmap);
            //Image image = new Image();
            //image.Source = _bitmap;
            //return image;
        }

        private BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }
    }
}
