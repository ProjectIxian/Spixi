using SPIXI.Interfaces;
using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;

[assembly: Dependency(typeof(PushService_iOS))]

public class PushService_iOS : IPushService
{
    public void initialize()
    {

    }

    public void setTag(string tag)
    {

    }

    public void clearNotifications()
    {

    }

    public void showLocalNotification(string title, string message, string data)
    {

    }
}