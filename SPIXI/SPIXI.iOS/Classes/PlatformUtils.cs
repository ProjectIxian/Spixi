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
            return new FileStream(path, FileMode.Open);
        }

        public string getAssetsBaseUrl()
        {
            return Path.Combine(NSBundle.MainBundle.BundlePath, "Resources") + "/";
        }

        public string getAssetsPath()
        {
            return Path.Combine(NSBundle.MainBundle.BundlePath, "Resources");
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