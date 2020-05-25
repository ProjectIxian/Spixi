using SPIXI.Interfaces;
using SPIXI.Meta;
using System;
using System.Linq;
using System.Text;
using System.Web;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SPIXI
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AppNewPage : SpixiContentPage
    {
        public AppNewPage()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            loadPage(webView, "app_new.html");
        }

        public override void recalculateLayout()
        {
            ForceLayout();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }


        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void onNavigating(object sender, WebNavigatingEventArgs e)
        {
            string current_url = HttpUtility.UrlDecode(e.Url);
            e.Cancel = true;

            if (current_url.StartsWith("ixian:onload", StringComparison.Ordinal))
            {
                onLoad();
            }
            else if (current_url.Equals("ixian:back", StringComparison.Ordinal))
            {
                onBack();
            }
            else if (current_url.StartsWith("ixian:install:"))
            {
                string url = current_url.Substring("ixian:install:".Length);
                onInstall(url);
            }
            else
            {
                // Otherwise it's just normal navigation
                e.Cancel = false;
                return;
            }
            e.Cancel = true;
        }

        private void onNavigated(object sender, WebNavigatedEventArgs e)
        {
            // Deprecated due to WPF, use onLoad
        }

        private void onLoad()
        {
            // Execute timer-related functionality immediately
            updateScreen();
        }

        // Executed every second
        public override void updateScreen()
        {

        }

        private void onInstall(string path)
        {
            string app_name = Node.customAppManager.install(path);
            if (app_name != null)
            {
                displaySpixiAlert("Spixi App Manager", "Application '" + app_name + "' successfully installed.", "OK");
                Navigation.PopAsync(Config.defaultXamarinAnimations);
            }
            else
            {
                displaySpixiAlert("Spixi App Manager", "Error occured while installing application.", "OK");
            }
        }

        private void onBack()
        {
            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        protected override bool OnBackButtonPressed()
        {
            onBack();

            return true;
        }
    }
}