using Pango;
using SPIXI.GTK.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;
using Xamarin.Forms.Platform.GTK.Renderers;

[assembly: ExportRenderer(typeof(WebView), typeof(SpixiWebViewRenderer))]

namespace SPIXI.GTK.Classes
{
    public class SpixiWebViewRenderer : WebViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.WebView> e)
        {
            base.OnElementChanged(e);
          
            if (Control != null)
            {
            }
        }

    }
}
