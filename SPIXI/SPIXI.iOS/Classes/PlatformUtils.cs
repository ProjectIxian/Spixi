using Foundation;
using SPIXI.Interfaces;
using SPIXI.iOS.Classes;
using SPIXI.Meta;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]

namespace SPIXI.iOS.Classes
{
    public class PlatformUtils : IPlatformUtils
    {
        public Stream getAsset(string path)
        {
            return new FileStream(Path.Combine(getAssetsPath(), path), FileMode.Open, FileAccess.Read);
        }

        public string getAssetsBaseUrl()
        {
            return NSBundle.MainBundle.BundlePath + "/";
        }

        public string getAssetsPath()
        {
            return NSBundle.MainBundle.BundlePath;
        }

        public string getHtmlBaseUrl()
        {
            return Config.spixiUserFolder + "/html/";
        }

        public string getHtmlPath()
        {
            return Config.spixiUserFolder + "/html";
        }
    }
}