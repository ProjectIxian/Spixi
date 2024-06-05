using IXICore;
using IXICore.Meta;
using System.IO;

namespace SPIXI.Meta
{
    public class Config
    {
        // Providing pre-defined values
        // Can be read from a file later, or read from the command line

        public static NetworkType networkType = NetworkType.main;

        public static bool enablePushNotifications = true;

        public static string walletFile = "wallet.ixi";

        public static string spixiUserFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Spixi");

        public static int encryptionRetryPasswordAttempts = 3;   // How many allowed attempts in the LaunchRetry page before throwing the user back to Launch Page

        // Read-only values
        public static readonly string aboutUrl = "https://www.spixi.io";
        public static readonly string guideUrl = "https://www.spixi.io/guide.html";
        public static readonly string explorerUrl = "https://explorer.ixian.io/";

        public static readonly string pushServiceUrl = "https://ipn.ixian.io/v1";
        public static readonly string priceServiceUrl = "https://www.ixian.io/ixiprice.txt";

        public static readonly int checkPriceSeconds = 1800; // 30 minutes

        public static readonly int packetDataSize = 102400; // 100 Kb per packet for file transfers
        public static readonly long packetRequestTimeout = 60; // Time in seconds to re-request packets

        public static readonly string version = "spixi-0.7.0"; // Spixi version

        public static readonly string checkVersionUrl = "https://www.ixian.io/spixi-update.txt";
        public static readonly int checkVersionSeconds = 1 * 60 * 60; // 1 hour

        public static readonly string bridgeAddress = ""; // IxiCash to WIXI bridge address

        // Default SPIXI settings
        public static bool defaultXamarinAnimations = false;
        public static uint messagesToLoad = 100; // Number of chat messages to load in each chunk

        // Push notifications OneSignal AppID
        public static string oneSignalAppId = "44d96ce3-5d33-4e8b-997d-d1ad786b96a1";

        // Temporary variables for bh sync recovery
        // Note: Always round last block height to 1000 and subtract 1 (i.e. if last block height is 33234, the correct value is 32999)
        public static ulong bakedRecoveryBlockHeight = 2929999;
        public static byte[] bakedRecoveryBlockChecksum = Crypto.stringToHash("707cbf6341464bba5e4e65b383c6c4ac371911f915269136607a7da847f99f362095b4d534999930c901dd41a8377b6af306d797a1b1acde71de71022c9728a5");

        // VoIP settings, don't change
        public static readonly int VoIP_sampleRate = 16000;
        public static readonly int VoIP_bitRate = 16;
        public static readonly int VoIP_channels = 1;
    }
}