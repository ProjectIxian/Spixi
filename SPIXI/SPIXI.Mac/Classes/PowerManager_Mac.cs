using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(PowerManager_Mac))]
public class PowerManager_Mac : IPowerManager
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