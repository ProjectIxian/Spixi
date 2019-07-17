using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(SystemAlert_WPF))]
public class SystemAlert_WPF : ISystemAlert
{
    public void displayAlert(string title, string message, string cancel)
    {
        MessageBoxResult result = MessageBox.Show(message,
                                                  title,
                                                  MessageBoxButton.OK,
                                                  MessageBoxImage.Exclamation);
    }

}