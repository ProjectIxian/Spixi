using IXICore.Meta;
using System;
using Xamarin.Forms;

namespace SPIXI
{
    public class Utils
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string UnixTimeStampToString(double unixTimeStamp)
        {
            DateTime datetime = UnixTimeStampToDateTime(unixTimeStamp);
            return datetime.ToString("MM/dd/yyyy HH:mm:ss");
        }

        public static string escapeHtmlParameter(string str)
        {
            return str.Replace("\"", "&#34;").Replace("'", "&#39;").Replace("\\", "&#92;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        public static void sendUiCommand(WebView webView, string command, params string[] arguments)
        {
            try
            {
                if(!webView.IsEnabled)
                {
                    return;
                }
                string cmd_str = command + "(";
                bool first = true;
                foreach (string arg in arguments)
                {
                    if (!first)
                    {
                        cmd_str += ",";
                    }
                    cmd_str += "'" + escapeHtmlParameter(arg) + "'";
                    first = false;
                }
                cmd_str += ");";

                // Call webview methods on the main UI thread only
                Xamarin.Forms.Device.BeginInvokeOnMainThread(() =>
                {
                    webView.Eval("try { " + cmd_str + " }catch(e){  }");
                });
            }catch(Exception e)
            {
                Logging.error("Exception occured in sendUiCommand " + e);
            }
        }
    }
}
