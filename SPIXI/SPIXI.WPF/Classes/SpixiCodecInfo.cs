using SPIXI.VoIP;
using SPIXI.WPF.Classes;
using System.Collections.Generic;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiCodecInfo))]
namespace SPIXI.WPF.Classes
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
            switch (codec_name)
            {
                case "opus":
                    return true;
            }
            return false;
        }
    }
}
