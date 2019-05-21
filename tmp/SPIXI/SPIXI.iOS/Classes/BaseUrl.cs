using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using SPIXI.Interfaces;
using SPIXI.iOS.Classes;
using UIKit;
using Xamarin.Forms;

[assembly: Dependency(typeof(BaseUrl_iOS))]

namespace SPIXI.iOS.Classes
{
    public class BaseUrl_iOS : IBaseUrl
    {
        public string Get()
        {
            //return string.Format("{0}/{1}/",NSBundle.MainBundle.BundlePath, "Resources");
            return string.Format("{0}/",NSBundle.MainBundle.BundlePath);
        }
    }
}