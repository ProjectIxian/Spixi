using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spixi
{
    public class SSpixiCodecInfo
    {
        static string[] codecMap = new string[] { "amrwb", "amrnb" };

        public static List<string> getSupportedAudioCodecs()
        {
            var cl = new List<string>();
            cl.Add("opus");
            return cl;
        }

        public static bool isCodecSupported(string codec_name)
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
