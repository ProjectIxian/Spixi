
namespace SPIXI.VoIP
{
    public static class CodecTools
    {
        public static int getPcmFrameByteSize(int samples, int bit_rate, int channels)
        {
            return channels * (bit_rate / 8) * samples / 1000;
        }
    }
}
