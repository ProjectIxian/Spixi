
using System.IO;

namespace SPIXI.Meta
{
    public class Config
    {
        // Providing pre-defined values
        // Can be read from a file later, or read from the command line

        public static bool isTestNet = true;

        public static bool enablePushNotifications = true; // Push notifications are disabled for now

        public static string walletFile = "wallet.ixi";

        public static string spixiUserFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Spixi");

        public static int encryptionRetryPasswordAttempts = 3;   // How many allowed attempts in the LaunchRetry page before throwing the user back to Launch Page

        // Read-only values
        public static readonly string aboutUrl = "https://www.spixi.io";

        public static readonly string pushServiceUrl = "https://ipn.ixian.io"; // Will be changed once the official push service is active

        public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

        public static readonly int packetDataSize = 102400; // 100 Kb per packet for file transfers
        public static readonly long packetRequestTimeout = 30; // Time in seconds to re-request packets

        public static readonly string version = "spixi-0.4.1-rc1"; // Spixi version

        // Default SPIXI settings
        public static bool defaultXamarinAnimations = false;

        // App-specific settings
        public static bool storeHistory = true;

        // Push notifications OneSignal AppID
        public static string oneSignalAppId = "44d96ce3-5d33-4e8b-997d-d1ad786b96a1";

        // Block height at which the current version of Spixi was generated
        // Useful for optimized block header sync
        public static ulong bakedBlockHeight = 800000;

        // Block checksum (paired with bakedBlockHeight) at which the current version of Spixi was generated
        // Useful for optimized block header sync
        public static byte[] bakedBlockChecksum = null;


        private Config()
        {

        }
    }
}