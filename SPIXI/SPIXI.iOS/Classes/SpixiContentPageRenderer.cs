using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using SPIXI;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using CoreGraphics;

[assembly: ExportRenderer(typeof(SpixiContentPage), typeof(SpixiContentPageRenderer))]

namespace SPIXI
{
    public class SpixiContentPageRenderer : PageRenderer
    {
        private double originalSize;
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
            
            var frameBegin = UIKeyboard.FrameBeginFromNotification(notification);
            var frameEnd = UIKeyboard.FrameEndFromNotification(notification);


            /*
            UIView activeView = KeyboardGetActiveView();
            if (activeView == null)
                return;

            UIScrollView scrollView = activeView.FindSuperviewOfType(this.View, typeof(UIScrollView)) as UIScrollView;
            if (scrollView == null)
                return;

            RectangleF keyboardBounds = (RectangleF)UIKeyboard.FrameBeginFromNotification(notification);

            UIEdgeInsets contentInsets = new UIEdgeInsets(0.0f, 0.0f, keyboardBounds.Size.Height, 0.0f);
            scrollView.ContentInset = contentInsets;
            scrollView.ScrollIndicatorInsets = contentInsets;

            // If activeField is hidden by keyboard, scroll it so it's visible
            CGRect viewRectAboveKeyboard = new CGRect(this.View.Frame.Location, new CGSize(this.View.Frame.Width, this.View.Frame.Size.Height - keyboardBounds.Size.Height));

            RectangleF activeFieldAbsoluteFrame = (RectangleF)activeView.Superview.ConvertRectToView(activeView.Frame, this.View);
            // activeFieldAbsoluteFrame is relative to this.View so does not include any scrollView.ContentOffset

            // Check if the activeField will be partially or entirely covered by the keyboard
            if (!viewRectAboveKeyboard.Contains(activeFieldAbsoluteFrame))
            {
                // Scroll to the activeField Y position + activeField.Height + current scrollView.ContentOffset.Y - the keyboard Height
                CGPoint scrollPoint = new CGPoint(0.0f, activeFieldAbsoluteFrame.Location.Y + activeFieldAbsoluteFrame.Height + scrollView.ContentOffset.Y - viewRectAboveKeyboard.Height);
                scrollView.SetContentOffset(scrollPoint, true);
            }*/


            /*    var page = Element as ContentPage;
                if (page != null && !(page.Content is ScrollView))
                {

                    var padding = page.Padding;
                    page.Padding = new Thickness(padding.Left, padding.Top, padding.Right, padding.Bottom + frameBegin.Top - frameEnd.Top);
                }
                */

            var bounds = Element.Bounds;
            var newBounds = new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height - frameBegin.Top + frameEnd.Top);
            Element.Layout(newBounds);

            var cp = Element as SpixiContentPage;
            if (cp != null)
            {
                cp.recalculateLayout();
            }


            /*  //Check if the keyboard is becoming visible
              var visible = notification.Name == UIKeyboard.WillShowNotification;

              //Pass the notification, calculating keyboard height, etc.
              bool landscape = InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft || InterfaceOrientation == UIInterfaceOrientation.LandscapeRight;
              var keyboardFrame = visible
                  ? UIKeyboard.FrameEndFromNotification(notification)
                  : UIKeyboard.FrameBeginFromNotification(notification);

              //OnKeyboardChanged(visible, landscape ? keyboardFrame.Width : keyboardFrame.Height);*/
        }


    }
}