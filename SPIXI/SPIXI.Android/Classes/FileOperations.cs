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
using Android.Support.V4.Content;
using Java.IO;

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
        File file = new File(filepath);
        Intent shareIntent = new Intent();
        shareIntent.SetAction(Intent.ActionSend);
        shareIntent.SetType("application/octetstream");
        Android.Net.Uri uriShare = FileProvider.GetUriForFile(_context, "com.ixian.provider", file);
        shareIntent.PutExtra(Intent.ExtraStream, uriShare);

        var chooserIntent = Intent.CreateChooser(shareIntent, title ?? string.Empty);
        chooserIntent.SetFlags(ActivityFlags.ClearTop);
        chooserIntent.SetFlags(ActivityFlags.NewTask);
        _context.StartActivity(chooserIntent);

        return Task.FromResult(true);
    }

}