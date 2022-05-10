using SPIXI.Lang;
using System.Windows.Shell;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace SPIXI.WPF
{

    public class SpixiApplicationPage : FormsApplicationPage
    {

    }

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

            SpixiLocalization.addCustomString("Platform", "Xamarin-WPF");

            LoadApplication(SPIXI.App.Instance());
        }
    }
}
