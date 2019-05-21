using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(BaseUrl_WPF))]


public class BaseUrl_WPF : IBaseUrl
{
    public string Get()
    {
        return "pack://siteoforigin:,,,/";
    }
}