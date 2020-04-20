using System.Collections.Generic;
using SPIXI.iOS.Classes;
using SPIXI.VoIP;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiCodecs))]
namespace SPIXI.iOS.Classes
{
    class SpixiCodecs : ISpixiCodecs
    {
        public List<string> getSupportedAudioCodecs()
        {
            // TODO implement
            var cl = new List<string>();
            cl.Add("AMR-NB");
            cl.Add("PCM");
            return cl;
        }

        public bool isCodecSupported(string codec_name)
        {
            // TODO implement
            switch (codec_name)
            {
                case "AMR-NB":
                    return true;
                case "PCM":
                    return true;
            }
            return false;
        }
    }
}