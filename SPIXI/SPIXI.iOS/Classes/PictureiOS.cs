using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using SPIXI.Interfaces;
using SPIXI.iOS.Classes;
using Xamarin.Forms;

[assembly: Xamarin.Forms.Dependency(typeof(PictureiOS))]

namespace SPIXI.iOS.Classes
{
    public class PictureiOS : IPicture
    {
        public void writeToGallery(string filename, byte[] imageData)
        {
            var p_image = new UIImage(NSData.FromArray(imageData));
            p_image.SaveToPhotosAlbum((image, error) =>
            {
                //var i = image as UIImage;
                if (error != null)
                {
                    Console.WriteLine(error.ToString());
                }
            });
        }

    }
}