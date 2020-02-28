using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using SPIXI.Interfaces;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using System.Threading.Tasks;

[assembly: Dependency(typeof(FileOperations_iOS))]

public class FileOperations_iOS : IFileOperations
{
    public void open(string filepath)
    {
        throw new NotImplementedException();
    }

    public Task share(string filepath, string title)
    {
        var items = new NSObject[] { NSObject.FromObject(title), NSUrl.FromFilename(filepath) };
        var activityController = new UIActivityViewController(items, null);
        var vc = GetVisibleViewController();

        NSString[] excludedActivityTypes = null;

        if (excludedActivityTypes != null && excludedActivityTypes.Length > 0)
            activityController.ExcludedActivityTypes = excludedActivityTypes;

        if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
        {
            if (activityController.PopoverPresentationController != null)
            {
                activityController.PopoverPresentationController.SourceView = vc.View;
            }
        }
        vc.PresentViewControllerAsync(activityController, true);
        return Task.FromResult(true);
    }

    UIViewController GetVisibleViewController()
    {
        var rootController = UIApplication.SharedApplication.KeyWindow.RootViewController;

        if (rootController.PresentedViewController == null)
            return rootController;

        if (rootController.PresentedViewController is UINavigationController)
        {
            return ((UINavigationController)rootController.PresentedViewController).TopViewController;
        }

        if (rootController.PresentedViewController is UITabBarController)
        {
            return ((UITabBarController)rootController.PresentedViewController).SelectedViewController;
        }

        return rootController.PresentedViewController;
    }
}