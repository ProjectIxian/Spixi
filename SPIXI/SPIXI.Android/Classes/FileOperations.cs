using Android.Content;
using SPIXI.Interfaces;
using Xamarin.Forms;
using System.Threading.Tasks;
using Android.Support.V4.Content;
using SPIXI.Droid;
using Java.IO;
using Android.Webkit;

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
        shareIntent.SetType("application/octet-stream");
        Android.Net.Uri uriShare = FileProvider.GetUriForFile(_context, "com.ixian.provider", file);
        shareIntent.PutExtra(Intent.ExtraStream, uriShare);

        var chooserIntent = Intent.CreateChooser(shareIntent, title ?? string.Empty);
        chooserIntent.SetFlags(ActivityFlags.ClearTop);
        chooserIntent.SetFlags(ActivityFlags.NewTask);
        _context.StartActivity(chooserIntent);

        return Task.FromResult(true);
    }

    public string getMimeType(Android.Net.Uri uri)
    {
        string mime_type = null;
        if (uri.Scheme.Equals(ContentResolver.SchemeContent))
        {
            ContentResolver cr = MainActivity.Instance.ContentResolver;
            mime_type = cr.GetType(uri);
        }
        else
        {
            string ext = MimeTypeMap.GetFileExtensionFromUrl(uri.ToString());
            mime_type = MimeTypeMap.Singleton.GetMimeTypeFromExtension(ext.ToLower());
        }
        if (mime_type == null)
        {
            mime_type = "*/*";
        }
        return mime_type;
    }

    public void open(string file_path)
    {
        var context = MainActivity.Instance;

        File f = new File(context.FilesDir, System.IO.Path.Combine("Spixi", "Downloads", System.IO.Path.GetFileName(file_path)));
        if (f == null || !f.Exists())
        {
            f = new File(file_path);
        }
        Android.Net.Uri file_uri = FileProvider.GetUriForFile(context, "com.ixian.provider", f);

        string mime_type = getMimeType(file_uri);

        Intent intent = new Intent(Intent.ActionView);
        intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
        intent.SetDataAndType(file_uri, mime_type);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);

        context.StartActivity(intent);
    }


}