using System;
using Foundation;
using UIKit;
using SPIXI;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(SpixiContentPage), typeof(SpixiContentPageRenderer))]

namespace SPIXI
{
    public class SpixiContentPageRenderer : PageRenderer
    {
        NSObject observerHideKeyboard;
        NSObject observerShowKeyboard;

        
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Set the background color to the default SPIXI colorscheme
            View.BackgroundColor = UIColor.FromRGB(173, 0, 87);

            var cp = Element as SpixiContentPage;
            if (cp != null && !cp.CancelsTouchesInView)
            {
                foreach (var g in View.GestureRecognizers)
                {
                    g.CancelsTouchesInView = false;
                }
            }
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            observerHideKeyboard = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillHideNotification, OnKeyboardNotification);
            observerShowKeyboard = NSNotificationCenter.DefaultCenter.AddObserver(UIKeyboard.WillShowNotification, OnKeyboardNotification);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);

            NSNotificationCenter.DefaultCenter.RemoveObserver(observerHideKeyboard);
            NSNotificationCenter.DefaultCenter.RemoveObserver(observerShowKeyboard);
        }

        void OnKeyboardNotification(NSNotification notification)
        {
            if (!IsViewLoaded) return;
        }

    }
}