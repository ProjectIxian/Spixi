using IXICore;
using IXICore.Meta;
#if WINDOWS
using Microsoft.Web.WebView2.Core;
#endif
using System;

namespace SPIXI
{
    public class Utils
    {
        public static DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string unixTimeStampToString(double unixTimeStamp)
        {
            DateTime datetime = unixTimeStampToDateTime(unixTimeStamp);
            return datetime.ToString("MM/dd/yyyy HH:mm:ss");
        }

        public static string unixTimeStampToHumanFormatString(double unixTimeStamp)
        {
            DateTime datetime = unixTimeStampToDateTime(unixTimeStamp);
            return datetime.ToString("dd MMM, yyyy, h:mm tt");
        }

        public static string escapeHtmlParameter(string str)
        {
            return str.Replace("\"", "&#34;").Replace("'", "&#39;").Replace("\\", "&#92;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\r\n", "\\n").Replace("\n", "\\n");
        }

        public static string amountToHumanFormatString(IxiNumber amount)
        {
            string amount_string = amount.ToString();
            if (amount > 1)
                return amount_string[..^6];
            return amount_string;
        }

        public static async Task<bool> winUIFix(WebView webView)
        {
#if WINDOWS_UIFIX
            var result = true;
            await  MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try {
                //await (webView.Handler.PlatformView as Microsoft.Maui.Platform.MauiWebView).EnsureCoreWebView2Async();
                CoreWebView2 cr = (webView.Handler.PlatformView as Microsoft.Maui.Platform.MauiWebView).CoreWebView2;
                if (cr == null)
                {              
                    result =  false;
                }
                //cr.Settings.AreWebSpecificContextMenuOptionsEnabled = false;
                }
                catch (Exception ex) {
                    result =  false;
                }
                
            });
            return result;
#endif
            return true;

        }

        public static void sendUiCommand(WebView webView, string command, params string[] arguments)
        {
            try
            {
                if(!webView.IsEnabled && !webView.IsLoaded)
                {
                    return;
                }

                if (!winUIFix(webView).Result)
                {
                    Logging.warn("Webview error for: " + command);
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


                MainThread.BeginInvokeOnMainThread(() =>
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
