using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.Interfaces
{
    public interface IPowerManager
    {
        bool AquireLock(string lock_type = "screenDim");
        bool ReleaseLock(string lock_type = "screenDim");
    }
}
