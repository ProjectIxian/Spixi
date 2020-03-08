using SPIXI.Droid;
using SPIXI.Interfaces;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(CustomQRScanner_Android))]
class CustomQRScanner_Android : ICustomQRScanner
{
    public async Task<bool> requestPermission()
    {
        bool r = await ZXing.Net.Mobile.Android.PermissionsHandler.RequestPermissionsAsync(MainActivity.Instance);
        return r;
    }

    public bool needsPermission()
    {
        return ZXing.Net.Mobile.Android.PermissionsHandler.NeedsPermissionRequest(MainActivity.Instance);
    }

    public bool useCustomQRScanner()
    {
        return false;
    }
}