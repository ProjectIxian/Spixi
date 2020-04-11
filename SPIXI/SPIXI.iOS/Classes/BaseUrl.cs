using SPIXI.Interfaces;
using SPIXI.iOS.Classes;
using SPIXI.Meta;
using Xamarin.Forms;

[assembly: Dependency(typeof(BaseUrl_iOS))]

namespace SPIXI.iOS.Classes
{
    public class BaseUrl_iOS : IBaseUrl
    {
        public string Get()
        {
            //return string.Format("{0}/{1}/",NSBundle.MainBundle.BundlePath, "Resources");
            return Config.spixiUserFolder + "/";
        }
    }
}