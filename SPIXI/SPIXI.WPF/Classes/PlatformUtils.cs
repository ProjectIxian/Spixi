using System.IO;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]


public class PlatformUtils : IPlatformUtils
{
    public Stream getAsset(string path)
    {
        return new FileStream(path, FileMode.Open);
    }

    public string getAssetsBaseUrl()
    {
        return "pack://siteoforigin:,,,/";
    }

    public string getAssetsPath()
    {
        return System.AppDomain.CurrentDomain.BaseDirectory;
    }

    public string getHtmlBaseUrl()
    {
        return getAssetsBaseUrl() + "html/";
    }

    public string getHtmlPath()
    {
        return Path.Combine(getAssetsPath(), "html");
    }
}