using SPIXI.Droid;
using SPIXI.Interfaces;
using System.IO;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]


public class PlatformUtils : IPlatformUtils
{
    public Stream getAsset(string path)
    {
        return MainActivity.Instance.Assets.Open(path);
    }

    public string getAssetsBaseUrl()
    {
        return "file:///android_asset/";
    }

    public string getAssetsPath()
    {
        throw new System.NotImplementedException();
    }

    public string getHtmlBaseUrl()
    {
        return SPIXI.Meta.Config.spixiUserFolder + "/html/";
    }

    public string getHtmlPath()
    {
        return SPIXI.Meta.Config.spixiUserFolder + "/html";
    }
}