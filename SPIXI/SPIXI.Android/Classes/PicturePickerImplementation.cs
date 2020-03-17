using System.IO;
using System.Threading.Tasks;
using Android.Content;
using SPIXI.Interfaces;
using SPIXI.Droid.Classes;
using Xamarin.Forms;
using Android.Graphics;

[assembly: Dependency(typeof(PicturePickerImplementation))]

namespace SPIXI.Droid.Classes
{
    public class PicturePickerImplementation : IPicturePicker
    {
        public Task<Stream> GetImageStreamAsync()
        {
            // Define the Intent for getting images
            Intent intent = new Intent();
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);

            // Get the MainActivity instance
            MainActivity activity = MainActivity.Instance;

            // Start the picture-picker activity (resumes in MainActivity.cs)
            activity.StartActivityForResult(
                Intent.CreateChooser(intent, "Select Picture"),
                MainActivity.PickImageId);

            // Save the TaskCompletionSource object as a MainActivity property
            activity.PickImageTaskCompletionSource = new TaskCompletionSource<Stream>();

            // Return Task object
            return activity.PickImageTaskCompletionSource.Task;
        }

        public byte[] ResizeImage(byte[] image_data, int new_width, int new_height)
        {
            Bitmap original_image = BitmapFactory.DecodeByteArray(image_data, 0, image_data.Length);

            if (original_image == null)
            {
                return null;
            }

            float width = original_image.Width;
            float height = original_image.Height;

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

            Bitmap resized_image = Bitmap.CreateScaledBitmap(original_image, resized_width, resized_height, false);
            Bitmap cropped_image = Bitmap.CreateBitmap(resized_image, margin_x, margin_y, new_width, new_height);

            using (MemoryStream ms = new MemoryStream())
            {
                cropped_image.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                return ms.ToArray();
            }
        }
    }
}