using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using SPIXI.Interfaces;
using SPIXI.WPF;
using Xamarin.Forms;

[assembly: Dependency(typeof(SystemAlert_WPF))]

public class SystemAlert_WPF : ISystemAlert
{

    private const UInt32 FLASHW_STOP = 0; // Stop flashing. The system restores the window to its original state.
    private const UInt32 FLASHW_CAPTION = 1; // Flash the window caption. 
    private const UInt32 FLASHW_TRAY = 2; // Flash the taskbar button. 
    private const UInt32 FLASHW_ALL = 3; // Flash both the window caption and taskbar button.
    private const UInt32 FLASHW_TIMER = 4; // Flash continuously, until the FLASHW_STOP flag is set.
    private const UInt32 FLASHW_TIMERNOFG = 12; // Flash continuously until the window comes to the foreground.

    private struct FLASHWINFO
    {
        public UInt32 cbSize; // The size of the structure, in bytes.
        public IntPtr hwnd; // A handle to the window to be flashed. The window can be either opened or minimized.
        public UInt32 dwFlags; // The flash status.
        public UInt32 uCount; // The number of times to flash the window.
        public UInt32 dwTimeout; // The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the default cursor blink rate.
    }


    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FLASHWINFO pfwi);

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

            FLASHWINFO info = new FLASHWINFO
            {
                hwnd = windowHandle,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = 0,
                dwTimeout = 0
            };

            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));

            FlashWindowEx(ref info);
        });         
    }



}