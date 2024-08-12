using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SPIXI.Interfaces
{
    public interface IFileOperations
    {
        Task share(string filepath, string title);
        void open(string filepath);
    }
}
