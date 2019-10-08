using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Windows.Threading;
using SPIXI.Interfaces;
using SPIXI.WPF;
using Xamarin.Forms;

[assembly: Dependency(typeof(SystemAlert_WPF))]

public class SystemAlert_WPF : ISystemAlert
{
    [DllImport("user32.dll")]
    public static extern int FlashWindow(IntPtr Hwnd, bool Revert);


    public void displayAlert(string title, string message, string cancel)
    {
        MessageBoxResult result = MessageBox.Show(message,
                                                  title,
                                                  MessageBoxButton.OK,
                                                  MessageBoxImage.Exclamation);
    }

    public void flash()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => {
            IntPtr windowHandle = new WindowInteropHelper(MainWindow.mainWindow).Handle;
            FlashWindow(windowHandle, true);
        });         
    }



}