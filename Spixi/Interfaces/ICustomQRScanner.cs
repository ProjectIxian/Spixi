using System.Threading.Tasks;

namespace SPIXI.Interfaces
{
    public interface ICustomQRScanner
    {
        Task<bool> requestPermission();
        bool needsPermission();
        bool useCustomQRScanner();
    }
}
