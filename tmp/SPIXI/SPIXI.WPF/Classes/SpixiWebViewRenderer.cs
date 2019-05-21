using SPIXI.WPF.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]

namespace SPIXI.WPF.Classes
{
    public class SpixiWebViewRenderer : Xamarin.Forms.Platform.WPF.WebViewRenderer
    {



    }
}
