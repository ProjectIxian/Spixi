using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Foundation;
using IXICore.Meta;
using SPIXI.Interfaces;
using SPIXI.iOS.Classes;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(PicturePickerImplementation))]

namespace SPIXI.iOS.Classes
{
    public class PicturePickerImplementation : IPicturePicker
    {
        TaskCompletionSource<Stream> taskCompletionSource;
        UIImagePickerController imagePicker;

        public Task<Stream> GetImageStreamAsync()
        {
            // Create and define UIImagePickerController
            imagePicker = new UIImagePickerController
            {
                SourceType = UIImagePickerControllerSourceType.PhotoLibrary,
                MediaTypes = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary)
            };

            // Set event handlers
            imagePicker.FinishedPickingMedia += OnImagePickerFinishedPickingMedia;
            imagePicker.Canceled += OnImagePickerCancelled;

            // Present UIImagePickerController;
            UIWindow window = UIApplication.SharedApplication.KeyWindow;
            var viewController = window.RootViewController;
            viewController.PresentModalViewController(imagePicker, true);

            // Return Task object
            taskCompletionSource = new TaskCompletionSource<Stream>();
            return taskCompletionSource.Task;
        }

        void OnImagePickerFinishedPickingMedia(object sender, UIImagePickerMediaPickedEventArgs args)
        {
            UIImage image = args.EditedImage ?? args.OriginalImage;

            if (image != null)
            {
                // Convert UIImage to .NET Stream object
                NSData data = image.AsJPEG(1);
                Stream stream = data.AsStream();

                // Set the Stream as the completion of the Task
                taskCompletionSource.SetResult(stream);
            }
            else
            {
                taskCompletionSource.SetResult(null);
            }
            imagePicker.DismissModalViewController(true);
        }

        void OnImagePickerCancelled(object sender, EventArgs args)
        {
            taskCompletionSource.SetResult(null);
            imagePicker.DismissModalViewController(true);
        }

        public byte[] ResizeImage(byte[] image_data, int new_width, int new_height)
        {
            UIImage original_image = ImageFromByteArray(image_data);

            if(original_image == null)
            {
                return null;
            }

            nfloat width = original_image.Size.Width;
            nfloat height = original_image.Size.Height;

            float resized_width = new_width;
            float resized_height = new_height;

            int margin_x = 0;
            int margin_y = 0;

            if (height > width)
            {
                nfloat ratio = height / new_height;
                resized_width = (int)(width / ratio);
                margin_x = (int)((resized_width - new_width) / 2);
            }
            else
            {
                nfloat ratio = width / new_width;
                resized_height = (int)(height / ratio);
                margin_y = (int)((resized_height - new_height) / 2);
            }

            // TODO crop as well

            UIGraphics.BeginImageContext(new SizeF(new_width, new_height));
            original_image.Draw(new RectangleF(0, 0, new_width, new_height));
            var resized_image = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();

            var bytes_imagen = resized_image.AsJPEG().ToArray();
            resized_image.Dispose();
            return bytes_imagen;
        }

        public static UIKit.UIImage ImageFromByteArray(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            UIKit.UIImage image;
            try
            {
                image = new UIKit.UIImage(Foundation.NSData.FromArray(data));
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in ImageFromBytes: " + e);
                return null;
            }

            return image;
        }
    }
}