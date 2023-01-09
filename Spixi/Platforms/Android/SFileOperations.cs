using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Webkit;
using IXICore.Meta;
using Java.IO;
using File = Java.IO.File;

namespace Spixi
{
    public class SFileOperations
    {
        public static Task share(string filepath, string title)
        {
            var context = MainActivity.Instance;
            File file = new File(filepath);
            Intent shareIntent = new Intent();
            shareIntent.SetAction(Intent.ActionSend);
            shareIntent.SetType("application/octet-stream");
            Android.Net.Uri uriShare = FileProvider.GetUriForFile(context, "com.ixian.provider", file);
            shareIntent.PutExtra(Intent.ExtraStream, uriShare);

            var chooserIntent = Intent.CreateChooser(shareIntent, title ?? string.Empty);
            chooserIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            context.StartActivity(chooserIntent);

            return Task.FromResult(true);
        }

        public static string getMimeType(Android.Net.Uri uri)
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

        public static void open(string file_path)
        {
            var context = MainActivity.Instance;
            File f;
            if (file_path.StartsWith(context.FilesDir.AbsolutePath))
            {
                f = new File(context.FilesDir, file_path.Substring(context.FilesDir.AbsolutePath.Length));
            }
            else
            {
                f = new File(file_path);
            }

            if (f == null || !f.Exists())
            {
                return;
            }

            try
            {
                Android.Net.Uri file_uri = FileProvider.GetUriForFile(context, "com.ixian.provider", f);

                string mime_type = getMimeType(file_uri);

                Intent intent = new Intent(Intent.ActionView);
                intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask | ActivityFlags.GrantReadUriPermission);
                intent.SetDataAndType(file_uri, mime_type);

                context.StartActivity(Intent.CreateChooser(intent, "Open file with"));
            }
            catch (Exception e)
            {
                Logging.error("Exception occured while trying to open file " + e);
            }
        }


    }
}
