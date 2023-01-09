using IXICore;
using IXICore.Meta;
using Spixi;
using SPIXI.Interfaces;
using SPIXI.Lang;
using SPIXI.Meta;
using SPIXI.Network;
using SPIXI.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainFlyoutPage : FlyoutPage
	{
		public MainFlyoutPage()
		{
            InitializeComponent();
            Flyout = HomePage.Instance();
        }

        void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Detail = new NavigationPage(new SettingsPage());
            IsPresented = false;            
        }
    }
}