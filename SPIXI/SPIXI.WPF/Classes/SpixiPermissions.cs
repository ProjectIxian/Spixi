using SPIXI.WPF.Classes;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiPermissions))]
namespace SPIXI.WPF.Classes
{
    class SpixiPermissions : ISpixiPermissions
    {
        public void requestAudioRecordingPermissions()
        {
        }
    }
}