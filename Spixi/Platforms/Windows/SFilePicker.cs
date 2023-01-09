using IXICore.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Spixi
{
    public class SFilePicker
    {
        static TaskCompletionSource<SpixiImageData> taskCompletionSource;

        public static Task<SpixiImageData> PickImageAsync()
        {
            // Return Task object
            taskCompletionSource = new TaskCompletionSource<SpixiImageData>();
            return taskCompletionSource.Task;
        }

        public static async Task<SpixiImageData> PickFileAsync()
        {
            FileResult fileData = await FilePicker.PickAsync();
            if (fileData == null)
                return null; // User canceled file picking

            SpixiImageData spixi_img_data = new SpixiImageData() { name = Path.GetFileName(fileData.FullPath), path = fileData.FullPath, stream = await fileData.OpenReadAsync() };

            // Return Task object
            return spixi_img_data;
        }

        

        public static byte[] ResizeImage(byte[] image_data, int new_width, int new_height, int quality)
        {
                return null;
        }

      
    }
}
