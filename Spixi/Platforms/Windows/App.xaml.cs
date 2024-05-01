using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SPIXI.Lang;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Spixi.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    const int WindowWidth = 450*2;
    const int WindowHeight = 700*2;
    const int MinWidth = 300*2;
    const int MinHeight = 420*2;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
	{
		this.InitializeComponent();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
            var mauiWindow = handler.VirtualView;
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();
            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));

            appWindow.Changed += (sender, args) =>
            {
                if (appWindow.Size.Width < MinWidth || appWindow.Size.Height < MinHeight)
                {
                    var newSize = new SizeInt32
                    {
                        Width = Math.Max(appWindow.Size.Width, MinWidth),
                        Height = Math.Max(appWindow.Size.Height, MinHeight)
                    };
                    appWindow.Resize(newSize);
                }
            };
        });

        SpixiLocalization.addCustomString("Platform", "Xamarin-WPF");

        // Add prepare storage (copy/overwrite html folder with embedded one)
        copyResources();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

    }

    public void copyResources()
    {
        string sourceDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html");
        string targetDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Spixi", "html");

        copyContents(sourceDirectory, targetDirectory);
    }

    private void copyContents(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (string file in Directory.GetFiles(sourceDirectory))
        {
            string destFile = Path.Combine(targetDirectory, Path.GetFileName(file));
            File.Copy(file, destFile, true); // overwrite existing files
        }

        foreach (string subdir in Directory.GetDirectories(sourceDirectory))
        {
            string destSubdir = Path.Combine(targetDirectory, Path.GetFileName(subdir));
            copyContents(subdir, destSubdir);
        }
    }

}

