using System.Collections.Generic;
using System.Linq;
using Android.Media;
using IXICore.Meta;
using SPIXI.Droid.Classes;
using SPIXI.VoIP;
using Xamarin.Forms;

[assembly: Dependency(typeof(SpixiCodecInfo))]
namespace SPIXI.Droid.Classes
{
    class SpixiCodecInfo : ISpixiCodecInfo
    {
        string[] codecMap = new string[] { "amrwb", "amrnb", "opus" };
        List<string> cachedCodecs = null;

        public List<string> getSupportedAudioCodecs()
        {
            if(cachedCodecs != null)
            {
                return cachedCodecs;
            }

            var mcl = new MediaCodecList(MediaCodecListKind.AllCodecs);
            var codec_infos = mcl.GetCodecInfos().ToList();


            var cl = new List<string>();

            foreach (var codec_in_map in codecMap)
            {
                var codec = codec_infos.Find(x => x.IsEncoder && x.Name.Contains(codec_in_map, System.StringComparison.OrdinalIgnoreCase));
                if (codec != null)
                {
                    cl.Add(codec_in_map);
                }
            }
            cachedCodecs = cl;
            //cl.Add("opus");
            return cl;
        }

        public bool isCodecSupported(string codec_name)
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