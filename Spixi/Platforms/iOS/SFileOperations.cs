using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Spixi
{
    public class FileInteractionDelegate : UIDocumentInteractionControllerDelegate
    {
        UIViewController parent;

        public FileInteractionDelegate(UIViewController controller)
        {
            parent = controller;
        }

        public override UIViewController ViewControllerForPreview(UIDocumentInteractionController controller)
        {
            return parent;
        }
    }

    public class SFileOperations
    {
        public static void open(string filepath)
        {
            var previewController = UIDocumentInteractionController.FromUrl(NSUrl.FromFilename(filepath));
            previewController.Delegate = new FileInteractionDelegate(GetVisibleViewController());
            previewController.PresentPreview(true);
        }

        public static Task share(string filepath, string title)
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

        static UIViewController GetVisibleViewController()
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
}
