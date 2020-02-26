using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SPIXI.Interfaces;
using SPIXI.Droid.Classes;
using Xamarin.Forms;
using System.IO;
using IXICore.Meta;

[assembly: Xamarin.Forms.Dependency(typeof(PictureAndroid))]
namespace SPIXI.Droid.Classes
{
    class PictureAndroid : IPicture
    {
        public void writeToGallery(string filename, byte[] imageData)
        {
            var dir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDcim);
            var pictures = dir.AbsolutePath;

            // Add a timestamp to the file to prevent overwriting
            string name = filename + System.DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg";
            string filePath = System.IO.Path.Combine(pictures, name);
            try
            {

                System.IO.File.WriteAllBytes(filePath, imageData);
                var mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                mediaScanIntent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(filePath)));
                MainActivity.Instance.SendBroadcast(mediaScanIntent);
                
            }
            catch (System.Exception e)
            {
                Logging.error(e.ToString());
            }
        }
    }
}