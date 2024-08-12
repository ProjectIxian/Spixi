using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;

namespace Spixi
{
    public class SPowerManager

    {
        public static bool AquireLock(string lock_type = "screenDim")
        {
            return true;
        }

        public static bool ReleaseLock(string lock_type = "screenDim")
        {
            return true;
        }

    }
}
