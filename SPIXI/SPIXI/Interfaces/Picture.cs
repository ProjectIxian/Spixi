using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.Interfaces
{
    public interface IPicture
    {
        void writeToGallery(string filename, byte[] imageData);
    }
}
