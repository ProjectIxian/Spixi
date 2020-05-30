using System.IO;

namespace SPIXI.Interfaces
{
    public interface IPlatformUtils
    {
        string getAssetsBaseUrl();
        string getAssetsPath();
        Stream getAsset(string path);
        string getHtmlPath();
        string getHtmlBaseUrl();
    }
}
