using System.Collections.Generic;
using SPIXI.iOS.Classes;
using SPIXI.VoIP;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiCodecInfo))]
namespace SPIXI.iOS.Classes
{
    class SpixiCodecInfo : ISpixiCodecInfo
    {
        string[] codecMap = new string[] { "amrwb", "amrnb" };

        public List<string> getSupportedAudioCodecs()
        {
            var cl = new List<string>();
            cl.Add("opus");
            return cl;
        }

        public bool isCodecSupported(string codec_name)
        {
            // TODO implement
            switch (codec_name)
            {
                case "opus":
                    return true;
            }
            return false;
        }
    }
}