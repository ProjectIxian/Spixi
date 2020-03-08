using Android.Content;
using SPIXI.Interfaces;
using Xamarin.Forms;
using System.Threading.Tasks;
using Android.Support.V4.Content;
using SPIXI.Droid;
using Java.IO;
using System;
using IXICore.Meta;

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

    public void open(string filepath)
    {
        string mime_type = "";
        string extension = System.IO.Path.GetExtension(filepath);

        // get mimeTye
        switch (extension.ToLower())
        {
            case ".txt":
                mime_type = "text/plain";
                break;
            case ".doc":
            case ".docx":
                mime_type = "application/msword";
                break;
            case ".pdf":
                mime_type = "application/pdf";
                break;
            case ".xls":
            case ".xlsx":
                mime_type = "application/vnd.ms-excel";
                break;
            case ".jpg":
            case ".jpeg":
            case ".png":
                mime_type = "image/jpeg";
                break;
            default:
                mime_type = "*/*";
                break;
        }

        var context = MainActivity.Instance;

        File f = new File(context.FilesDir, System.IO.Path.Combine("Spixi", "Downloads", System.IO.Path.GetFileName(filepath)));
        if (f == null || !f.Exists())
        {
            f = new File(filepath);
        }
        Android.Net.Uri file_uri = FileProvider.GetUriForFile(context, "com.ixian.provider", f);

        Intent intent = new Intent(Intent.ActionView);
        intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
        intent.SetDataAndType(file_uri, mime_type);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

        context.StartActivity(intent);
    }


}