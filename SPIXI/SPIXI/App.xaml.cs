using IXICore;
using IXICore.Meta;
using SPIXI.Lang;
using SPIXI.Meta;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SPIXI
{
    public partial class App : Application
	{
        private static App _singletonInstance;

        public static App Instance(bool force_redraw = false)
        {
            if (_singletonInstance == null)
            {
                _singletonInstance = new App();
            }else if(force_redraw)
            {
                _singletonInstance.redraw();
            }
            return _singletonInstance;
        }

        public static bool isInForeground { get; set; } = false;

        Node node = null;

        public static string startingScreen = ""; // Which screen to start on

		private App ()
		{
            InitializeComponent();

            // Fix for issue https://github.com/xamarin/Xamarin.Forms/issues/10712#issuecomment-629394090
            Device.SetFlags(new string[] { "anything" });
            
            // check if already started
            if (Node.Instance == null)
            {
                // Prepare the personal folder
                if (!Directory.Exists(Config.spixiUserFolder))
                {
                    Directory.CreateDirectory(Config.spixiUserFolder);
                }

                // Init logging
                Logging.setOptions(5, 1, true);
                Logging.start(Config.spixiUserFolder);
                Logging.info(string.Format("Starting Spixi {0} ({1})", Config.version, CoreConfig.version));

                // Init fatal exception handlers
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

                // Load or generate a device ID.
                if (Application.Current.Properties.ContainsKey("uid"))
                {
                    byte[] uid = Application.Current.Properties["uid"] as byte[];
                    if(uid == null)
                    {
                        // Generate and save the device ID
                        Application.Current.Properties["uid"] = CoreConfig.device_id;
                    }else
                    {
                        CoreConfig.device_id = uid;
                    }
                }
                else
                {
                    // Generate and save the device ID
                    Application.Current.Properties["uid"] = CoreConfig.device_id;
                }

                if(Application.Current.Properties.ContainsKey("language"))
                {
                    if(!SpixiLocalization.loadLanguage(Application.Current.Properties["language"] as string))
                    {
                        Application.Current.Properties["language"] = SpixiLocalization.getCurrentLanguage();
                        Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF
                    }
                }else
                {
                    string lang = CultureInfo.CurrentCulture.Name.ToLower();
                    if (SpixiLocalization.loadLanguage(lang))
                    {
                        Xamarin.Forms.Application.Current.Properties["language"] = SpixiLocalization.getCurrentLanguage();
                        Xamarin.Forms.Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF
                    }
                }

                movePersonalFiles();

                // Load theme and appearance
                ThemeAppearance themeAppearance = ThemeAppearance.automatic;
                if (Application.Current.Properties.ContainsKey("appearance"))
                {
                    themeAppearance = (ThemeAppearance)Current.Properties["appearance"];
                }
                ThemeManager.loadTheme("spixiui", themeAppearance);
                Current.RequestedThemeChanged += (s, a) =>
                {
                    // Respond to the theme change
                    Current.UserAppTheme = a.RequestedTheme;
                    if(ThemeManager.getActiveAppearance() == ThemeAppearance.automatic)
                        ThemeManager.changeAppearance(ThemeAppearance.automatic);

                    UIHelpers.reloadAllPages();
                };

                // Start Ixian code
                node = new Node();

                // Attempt to load a pre-existing wallet
                bool wallet_found = Node.checkForExistingWallet();

                if (!wallet_found)
                {
                    // Wallet not found, go to initial launch page
                    MainPage = new NavigationPage(new SPIXI.LaunchPage());
                }
                else
                {
                    // Wallet found, see if it can be decrypted
                    bool wallet_decrypted = IxianHandler.getWalletList().Count > 0 ? IxianHandler.getWalletStorage().isLoaded() : false;
                    if (!wallet_decrypted)
                    {
                        wallet_decrypted = Node.loadWallet();
                    }

                    if (wallet_decrypted == false)
                    {
                        MainPage = new NavigationPage(new SPIXI.LaunchRetryPage());
                    }
                    else
                    {
                        // Wallet found, go to main page

                        MainPage = new NavigationPage(HomePage.Instance());
                        //MainPage = new NavigationPage(new SPIXI.LockPage());
                    }
                }
                NavigationPage.SetHasNavigationBar(MainPage, false);
            }
            else
            {
                // Already started before
                node = Node.Instance;
            }
        }

        // Workaround for Android - sometimes the screen is empty when waking up for some Launch Modes
        public void redraw()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (MainPage != null && ((NavigationPage)MainPage).RootPage.GetType() == typeof(HomePage))
                {
                    MainPage = new NavigationPage(HomePage.Instance(true));
                    NavigationPage.SetHasNavigationBar(MainPage, false);
                }
            });
        }

        private void movePersonalFiles()
        {
            string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            if (File.Exists(Path.Combine(path, "spixi.wal")) && !File.Exists(Path.Combine(Config.spixiUserFolder, Config.walletFile)))
            {
                File.Move(Path.Combine(path, "spixi.wal"), Path.Combine(Config.spixiUserFolder, Config.walletFile));
            }
        }

        protected override void OnStart ()
		{
            // Handle when your app starts
            isInForeground = true;
            base.OnStart();
        }

		protected override void OnSleep ()
		{
            // Handle when your app sleeps
            isInForeground = false;
            Node.localStorage.flush();
            base.OnSleep();
        }

        protected override void OnResume ()
		{
            // Handle when your app resumes
            isInForeground = true;
            base.OnResume();

            if(MainPage != null && ((NavigationPage)MainPage).CurrentPage != null && ((NavigationPage)MainPage).CurrentPage is SpixiContentPage)
            {
                SpixiContentPage p = (SpixiContentPage)((NavigationPage)MainPage).CurrentPage;
                p.onResume();
            }
            OfflinePushMessages.lastUpdate = 0;
        }

        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            try
            {
                Logging.error(unobservedTaskExceptionEventArgs.Exception.ToString());
                Logging.flush();
            }
            catch
            {

            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            try
            {
                var e = unhandledExceptionEventArgs.ExceptionObject as Exception;
                Logging.error(e.ToString());
                Logging.flush();
            }
            catch
            {

            }
        }

        public void onLowMemory()
        {
            Node.Instance.onLowMemory();
        }

        public void flush()
        {
            Node.localStorage.flush();
        }

    }
}
