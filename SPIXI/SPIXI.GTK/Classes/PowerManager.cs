using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PowerManager_GTK))]
public class PowerManager_GTK : IPowerManager
{
    public bool AquireLock(string lock_type = "screenDim")
    {
        return true;
    }

    public bool ReleaseLock(string lock_type = "screenDim")
    {
        return true;
    }
}