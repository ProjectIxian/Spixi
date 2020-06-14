using System;
using System.IO;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PlatformUtils))]

public class PlatformUtils : IPlatformUtils
{
    public Stream getAsset(string path)
    {
        throw new NotImplementedException();
    }

    public string getAssetsBaseUrl()
    {
        throw new NotImplementedException();
    }

    public string getAssetsPath()
    {
        throw new NotImplementedException();
    }

    public string getHtmlBaseUrl()
    {
        throw new NotImplementedException();
    }

    public string getHtmlPath()
    {
        throw new NotImplementedException();
    }

    public void startRinging()
    {
        throw new NotImplementedException();
    }

    public void stopRinging()
    {
        throw new NotImplementedException();
    }

    public void startDialtone(DialtoneType type)
    {
        throw new NotImplementedException();
    }

    public void stopDialtone()
    {
        throw new NotImplementedException();
    }
}