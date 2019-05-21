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
using System.Threading.Tasks;

[assembly: Dependency(typeof(FileOperations_Android))]

public class FileOperations_Android : IFileOperations
{
    private readonly Context _context;
    public FileOperations_Android()
    {
        _context = Android.App.Application.Context;
    }

    public Task share(string filepath, string title)
    {
        var extension = filepath.Substring(filepath.LastIndexOf(".") + 1).ToLower();
        var contentType = string.Empty;

        // You can manually map more ContentTypes here if you want.
        switch (extension)
        {
            case "pdf":
                contentType = "application/pdf";
                break;
            case "png":
                contentType = "image/png";
                break;
            default:
                contentType = "application/octetstream";
                break;
        }

        var intent = new Intent(Intent.ActionSend);
        intent.SetType(contentType);
        intent.PutExtra(Intent.ExtraStream, Android.Net.Uri.Parse("file://" + filepath));
        intent.PutExtra(Intent.ExtraText, string.Empty);
        intent.PutExtra(Intent.ExtraSubject, string.Empty);

        var chooserIntent = Intent.CreateChooser(intent, title ?? string.Empty);
        chooserIntent.SetFlags(ActivityFlags.ClearTop);
        chooserIntent.SetFlags(ActivityFlags.NewTask);
        _context.StartActivity(chooserIntent);

        return Task.FromResult(true);
    }

}