using SPIXI.VoIP;
using SPIXI.WPF.Classes;
using System.Collections.Generic;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiCodecInfo))]
namespace SPIXI.WPF.Classes
{
    class SpixiCodecInfo : ISpixiCodecInfo
    {
        string[] codecMap = new string[] { "amrwb", "amrnb", "opus" };

        public List<string> getSupportedAudioCodecs()
        {
            // TODO implement
            var cl = new List<string>();
            //cl.Add("opus");
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
