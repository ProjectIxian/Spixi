using System.IO;

namespace SPIXI.Interfaces
{
    public enum DialtoneType
    {
        dialing,
        busy,
        error
    }

    public interface IPlatformUtils
    {
        string getAssetsBaseUrl();
        string getAssetsPath();
        Stream getAsset(string path);
        string getHtmlPath();
        string getHtmlBaseUrl();

        void startRinging();
        void stopRinging();

        void startDialtone(DialtoneType type);
        void stopDialtone();
    }
}
