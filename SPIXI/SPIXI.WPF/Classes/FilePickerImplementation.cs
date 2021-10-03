
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

[assembly: Dependency(typeof(FilePickerImplementation))]

namespace SPIXI.WPF.Classes
{
    public class FilePickerImplementation : IFilePicker
    {
        public Task<SpixiImageData> PickImageAsync()
        {
            OpenFileDialog file_dialog = new OpenFileDialog();
            file_dialog.Filter = "Image Files (*.jpeg, *.jpg, *.png)|*.jpeg;*.jpg;*.png";
            file_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SpixiImageData spixi_img_data = null;
            if (file_dialog.ShowDialog() == true)
            {
                spixi_img_data = new SpixiImageData() { name = Path.GetFileName(file_dialog.FileName), path = file_dialog.FileName, stream = File.OpenRead(file_dialog.FileName) };
            }

            // Return Task object
            return Task.FromResult(spixi_img_data);
        }

        public async Task<SpixiImageData> PickFileAsync()
        {
            OpenFileDialog file_dialog = new OpenFileDialog();
            file_dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            SpixiImageData spixi_img_data = null;
            if (file_dialog.ShowDialog() == true)
            {
                spixi_img_data = new SpixiImageData() { name = Path.GetFileName(file_dialog.FileName), path = file_dialog.FileName, stream = File.OpenRead(file_dialog.FileName) };
            }

            // Return Task object
            return spixi_img_data;
        }

        public byte[] ResizeImage(byte[] image_data, int new_width, int new_height, int quality)
        {
            BitmapImage original_image = new BitmapImage();
            original_image.BeginInit();
            original_image.StreamSource = new MemoryStream(image_data);
            original_image.EndInit();

            // Calculate crop section

            int orig_width = original_image.PixelWidth;
            int orig_height = original_image.PixelHeight;

            float width_ratio = (float)new_width / orig_width;
            float height_ratio = (float)new_height / orig_height;

            float ratio = Math.Max(width_ratio, height_ratio);

            int resized_pre_crop_width = (int)Math.Round(orig_width * ratio);
            int resized_pre_crop_height = (int)Math.Round(orig_height * ratio);
            
            // full area to crop on resized image
            int resized_crop_x = resized_pre_crop_width - new_width;
            int resized_crop_y = resized_pre_crop_height - new_height;

            int cropped_width = (int)((resized_pre_crop_width - resized_crop_x) / ratio);
            int cropped_height = (int)((resized_pre_crop_height - resized_crop_y) / ratio);

            // half of area to crop on original image
            int crop_x = (int)(resized_crop_x / ratio / 2);
            int crop_y = (int)(resized_crop_y / ratio / 2);

            // End of calculate crop section

            var cropped_image = new CroppedBitmap(original_image, new Int32Rect(crop_x, crop_y, cropped_width, cropped_height));

            var rect = new System.Windows.Rect(0, 0, new_width, new_height);

            var group = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
            group.Children.Add(new ImageDrawing(cropped_image, rect));


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
            jbe.QualityLevel = quality;

            using (MemoryStream stream = new MemoryStream())
            {
                jbe.Save(stream);
                image_data = stream.ToArray();
            }

            return image_data;
        }
    }
}