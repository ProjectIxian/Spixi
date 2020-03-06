using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

// Disable DPI awareness for now
[assembly: System.Windows.Media.DisableDpiAwareness]

namespace SPIXI.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public static MainWindow mainWindow = null;

        public MainWindow()
        {
            InitializeComponent();

            this.TaskbarItemInfo = new TaskbarItemInfo();
            mainWindow = this;

            Forms.Init();
            LoadApplication(SPIXI.App.Instance());
        }
    }
}
