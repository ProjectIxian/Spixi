using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spixi
{
    public class SPlatformUtils
    {
        public static Stream getAsset(string path)
        {
            Task<Stream> task = Task.Run<Stream>(async () => await FileSystem.Current.OpenAppPackageFileAsync(path));
            return task.Result;
        }

        public static string getAssetsBaseUrl()
        {
            return "pack://siteoforigin:,,,/";
        }

        public static string getAssetsPath()
        {
            return System.AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string getHtmlBaseUrl()
        {
            return Config.spixiUserFolder + "/html/";
        }

        public static string getHtmlPath()
        {
            return Config.spixiUserFolder + "/html";
        }

        public static void startRinging()
        {
        }

        public static void stopRinging()
        {
        }

        public static void startDialtone(DialtoneType type)
        {
        }

        public static void stopDialtone()
        {
        }

    }
}
