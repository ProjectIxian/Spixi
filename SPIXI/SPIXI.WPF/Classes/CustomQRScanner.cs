using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(CustomQRScanner_WPF))]
public class CustomQRScanner_WPF : ICustomQRScanner
{
    public bool useCustomQRScanner()
    {
        return true;
    }
}