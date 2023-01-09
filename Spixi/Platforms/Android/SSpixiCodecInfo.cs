using Android.Media;
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
        static List<string> cachedCodecs = null;

        public static List<string> getSupportedAudioCodecs()
        {
            if (cachedCodecs != null)
            {
                return cachedCodecs;
            }

            var mcl = new MediaCodecList(MediaCodecListKind.AllCodecs);
            var codec_infos = mcl.GetCodecInfos().ToList();


            var cl = new List<string>();
            cl.Add("opus");

            foreach (var codec_in_map in codecMap)
            {
                var codec = codec_infos.Find(x => x.IsEncoder && x.Name.Contains(codec_in_map, System.StringComparison.OrdinalIgnoreCase));
                if (codec != null)
                {
                    cl.Add(codec_in_map);
                }
            }
            cachedCodecs = cl;
            return cl;
        }

        public static bool isCodecSupported(string codec_name)
        {
            if (codec_name == "opus")
            {
                return true;
            }

            var mcl = new MediaCodecList(MediaCodecListKind.AllCodecs);
            var codec_infos = mcl.GetCodecInfos().ToList();
            var codec = codec_infos.Find(x => x.IsEncoder && x.Name.Contains(codec_name, System.StringComparison.OrdinalIgnoreCase));
            if (codec != null)
            {
                return true;
            }
            return false;
        }
    }
}
