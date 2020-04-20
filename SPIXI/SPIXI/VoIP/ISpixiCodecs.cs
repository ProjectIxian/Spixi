using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI.VoIP
{
    public interface ISpixiCodecs
    {
        List<string> getSupportedAudioCodecs();

        bool isCodecSupported(string codec_name);
    }
}
