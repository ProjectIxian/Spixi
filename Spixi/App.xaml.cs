using IXICore;
using IXICore.Meta;
using SPIXI;
using SPIXI.Lang;
using SPIXI.Meta;
using System.Globalization;

namespace Spixi;

public partial class App : Application
{
    private static App _singletonInstance;

    public static App Instance(bool force_redraw = false)
    {
        if (_singletonInstance == null)
        {
            _singletonInstance = new App();
        }

        return _singletonInstance;
    }
    public static bool isInForeground { get; set; } = false;

    Node node = null;

    public static Window appWindow { get; private set; } = null;

    public static string startingScreen = ""; // Which screen to start on

    private bool isLockScreenActive = false;
    private DateTime unlockedDate = DateTime.Now; // Store the last time when the app was unlocked via lockscreen


    public App()
	{
        InitializeComponent();
 

        // Fix for issue https://github.com/xamarin/Xamarin.Forms/issues/10712#issuecomment-629394090
        //Device.SetFlags(new string[] { "anything" });

        // check if already started
        if (Node.Instance == null)
        {
            // Prepare the personal folder
            if (!Directory.Exists(Config.spixiUserFolder))
            {
                Directory.CreateDirectory(Config.spixiUserFolder);
            }

            // Init logging
            Logging.setOptions(5, 1, false);
            Logging.start(Config.spixiUserFolder);
            Logging.info(string.Format("Starting Spixi {0} ({1})", Config.version, CoreConfig.version));

            // Init fatal exception handlers
            //AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            //TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;

            // Load or generate a device ID.
            if (Preferences.Default.ContainsKey("uid"))
            {
                /*
                byte[] uid = Preferences.Default.Get("uid", byte[]) as byte[];
                if (uid == null)
                {
                    // Generate and save the device ID
                    Application.Current.Properties["uid"] = CoreConfig.device_id;
                }
                else
                {
                    CoreConfig.device_id = uid;
                }*/
            }
            else
            {
                // Generate and save the device ID
                //Application.Current.Properties["uid"] = CoreConfig.device_id;
            }

            if (Preferences.Default.ContainsKey("language"))
            {
                if (!SpixiLocalization.loadLanguage(Preferences.Default.Get("language", "en") as string))
                {
                    Preferences.Default.Set("language", SpixiLocalization.getCurrentLanguage());
                }
            }
            else
            {
                string lang = CultureInfo.CurrentCulture.Name.ToLower();
                if (SpixiLocalization.loadLanguage(lang))
                {
                    Preferences.Default.Set("language", SpixiLocalization.getCurrentLanguage());
                }
            }

            movePersonalFiles();

            // Load theme and appearance
            //OSAppTheme currentTheme = Application.Current.RequestedTheme;
            //Current.UserAppTheme = currentTheme;
            ThemeAppearance themeAppearance = ThemeAppearance.automatic;
            if (Preferences.Default.ContainsKey("appearance"))
            {
                //themeAppearance = (ThemeAppearance)Preferences.Default.Get("appearance");
            }
            ThemeManager.loadTheme("spixiui", themeAppearance);
            Current.RequestedThemeChanged += (s, a) =>
            {
                // Respond to the theme change
                Current.UserAppTheme = a.RequestedTheme;
                if (ThemeManager.getActiveAppearance() == ThemeAppearance.automatic)
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
                    // Wallet found                     
                    if (isLockEnabled())
                    {
                        // Show the lock screen
                        isLockScreenActive = true;
                        var lockPage = new LockPage();
                        lockPage.authSucceeded += onUnlock;
                        MainPage = new NavigationPage(lockPage);
                    }
                    else
                    {
                        // Show the home screen
                        MainPage = new NavigationPage(HomePage.Instance());
                        //MainPage = new NavigationPage(new SPIXI.ScanPage());
                        //MainPage = new NavigationPage(new SPIXI.LaunchPage());
                        //MainPage = new MainFlyoutPage();


                    }

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

    // Check if the lock is enabled
    public bool isLockEnabled()
    {
        bool locked = false;
        if (Preferences.Default.ContainsKey("lockenabled"))
        {
            locked = (bool)Preferences.Default.Get("lockenabled", false);
        }
        return locked;
    }


    private void movePersonalFiles()
    {
        string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        if (File.Exists(Path.Combine(path, "spixi.wal")) && !File.Exists(Path.Combine(Config.spixiUserFolder, Config.walletFile)))
        {
            File.Move(Path.Combine(path, "spixi.wal"), Path.Combine(Config.spixiUserFolder, Config.walletFile));
        }
    }

    public void onUnlock(object sender, EventArgs e)
    {
        isLockScreenActive = false;
        unlockedDate = DateTime.Now;
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var window = base.CreateWindow(activationState);
        if (window != null)
        {
            window.Title = "Spixi IM";
            if (appWindow == null)
            {
                window.Destroying += (s, e) =>
                {
                    Shutdown();
                };
                appWindow = window;
            }
        }
        return window;
    }

    public static void Shutdown()
    {
        IxianHandler.shutdown();
        while (IxianHandler.status != NodeStatus.stopped)
        {
            Thread.Sleep(10);
        }
        Environment.Exit(0);
    }

}
