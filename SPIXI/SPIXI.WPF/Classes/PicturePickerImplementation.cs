
using IXICore.Meta;
using Microsoft.Win32;
using SPIXI.Interfaces;
using SPIXI.WPF.Classes;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xamarin.Forms;

[assembly: Dependency(typeof(PicturePickerImplementation))]

namespace SPIXI.WPF.Classes
{
    public class PicturePickerImplementation : IPicturePicker
    {
        public Task<Stream> GetImageStreamAsync()
        {
            OpenFileDialog file_dialog = new OpenFileDialog();
            file_dialog.Filter = "Image Files (*.jpeg, *.jpg, *.png)|*.jpeg;*.jpg;*.png";
            file_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Stream s = null;
            if (file_dialog.ShowDialog() == true)
            {
                s = File.OpenRead(file_dialog.FileName);
            }

            // Return Task object
            return Task.FromResult(s);
        }

        public byte[] ResizeImage(byte[] image_data, int new_width, int new_height)
        {
            BitmapImage original_image = new BitmapImage();
            original_image.BeginInit();
            original_image.StreamSource = new  MemoryStream(image_data);
            original_image.EndInit();

            float width = original_image.PixelWidth;
            float height = original_image.PixelHeight;

            int resized_width = new_width;
            int resized_height = new_height;

            int margin_x = 0;
            int margin_y = 0;

            if (height > width)
            {
                float ratio = height / new_height;
                resized_width = (int)(width / ratio);
                margin_x = (resized_width - new_width) / 2;
            }
            else
            {
                float ratio = width / new_width;
                resized_height = (int)(height / ratio);
                margin_y = (resized_height - new_height) / 2;
            }

            var rect = new Rect(0, 0, resized_width, resized_height);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(original_image, rect));

            var drawing_visual = new DrawingVisual();
            using (var drawing_context = drawing_visual.RenderOpen())
                drawing_context.DrawDrawing(group);

            var resized_image = new RenderTargetBitmap(
                new_width, new_height, // Resized dimensions
                96, 96,                // Default DPI values
                PixelFormats.Default); // Default pixel format
            resized_image.Render(drawing_visual);


            JpegBitmapEncoder jbe = new JpegBitmapEncoder();
            jbe.Frames.Add(BitmapFrame.Create(resized_image));
            jbe.QualityLevel = 100;

            using (MemoryStream stream = new MemoryStream())
            {
                jbe.Save(stream);
                image_data = stream.ToArray();
            }

            return image_data;
        }
    }
}