using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.Interfaces
{
    public interface IPowerManager
    {
        bool AquireLock();
        bool ReleaseLock();
    }
}
