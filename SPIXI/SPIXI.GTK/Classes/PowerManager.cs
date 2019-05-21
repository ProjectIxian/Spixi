using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PowerManager_WPF))]
public class PowerManager_WPF : IPowerManager
{
    public bool AquireLock()
    {
        return true;
    }

    public bool ReleaseLock()
    {
        return true;
    }
}