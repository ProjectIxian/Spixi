using System.IO;
using SPIXI.Interfaces;
using SPIXI.Mac;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]
namespace SPIXI.Mac
{
    public class PlatformUtils : IPlatformUtils
    {
        public PlatformUtils()
        {

        }

        public Stream getAsset(string path)
        {
            return new FileStream(path, FileMode.Open);
        }

        public string getAssetsBaseUrl()
        {
            return "";
        }

        public string getAssetsPath()
        {
            return Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Spixi");// System.AppDomain.CurrentDomain.BaseDirectory;
        }

        public string getHtmlBaseUrl()
        {
            return Path.Combine(getAssetsPath(), "html") + "/";
        }

        public string getHtmlPath()
        {
            return Path.Combine(getAssetsPath(), "html");
        }

        public void startRinging()
        {
        }

        public void stopRinging()
        {
        }

        public void startDialtone(DialtoneType type)
        {
        }

        public void stopDialtone()
        {
        }
    }
}