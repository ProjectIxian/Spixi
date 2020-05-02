using SPIXI.iOS.Classes;
using SPIXI.Interfaces;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiPermissions))]
namespace SPIXI.iOS.Classes
{
    class SpixiPermissions : ISpixiPermissions
    {
        public void requestAudioRecordingPermissions()
        {
            throw new System.NotImplementedException();
        }
    }
}