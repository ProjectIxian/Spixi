using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.Interfaces
{
    public interface ISystemAlert
    {
        void displayAlert(string title, string message, string cancel);
        void flash();

    }
}
