using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Platform.GTK;

namespace SPIXI.GTK
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Gtk.Application.Init();
            Forms.Init();

            var app = App.Instance();
            var window = new FormsWindow();
            window.LoadApplication(app);
            window.SetApplicationTitle("Spixi");
            window.Show();

            Gtk.Application.Run();
        }
    }
}
