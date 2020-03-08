using System.Threading.Tasks;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(CustomQRScanner_WPF))]
public class CustomQRScanner_WPF : ICustomQRScanner
{
    public bool needsPermission()
    {
        return false;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<bool> requestPermission()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return true;
    }

    public bool useCustomQRScanner()
    {
        return false;
    }
}