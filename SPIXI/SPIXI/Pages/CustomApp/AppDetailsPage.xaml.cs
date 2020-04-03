using SPIXI.CustomApps;
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
    public partial class AppDetailsPage : SpixiContentPage
    {
        string appId = null;

        public AppDetailsPage(string app_id)
        {
            InitializeComponent();

            appId = app_id;

            NavigationPage.SetHasNavigationBar(this, false);

            // Load the platform specific home page url
            var source = new UrlWebViewSource();
            source.Url = string.Format("{0}html/app_details.html", DependencyService.Get<IBaseUrl>().Get());
            webView.Source = source;
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
            else if (current_url.StartsWith("ixian:uninstall", StringComparison.Ordinal))
            {
                onUninstall();
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
            CustomApp app = Node.customAppManager.getApp(appId);

            string icon = Node.customAppManager.getAppIconPath(appId);
            if(icon == null)
            {
                icon = "";
            }

            Utils.sendUiCommand(webView, "init", app.name, icon, app.publisher, app.version);

            // Execute timer-related functionality immediately
            updateScreen();
        }

        private void onUninstall()
        {
            if(Node.customAppManager.remove(appId))
            {
                displaySpixiAlert("Spixi App Manager", "Application was successfully removed.", "OK");
            }else
            {
                displaySpixiAlert("Spixi App Manager", "An error occured while removing the application.", "OK");
            }
            Navigation.PopAsync(Config.defaultXamarinAnimations);
        }

        // Executed every second
        public override void updateScreen()
        {

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