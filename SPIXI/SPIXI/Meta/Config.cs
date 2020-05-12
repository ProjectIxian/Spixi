using IXICore;
using System.IO;

namespace SPIXI.Meta
{
    public class Config
    {
        // Providing pre-defined values
        // Can be read from a file later, or read from the command line

        public static bool isTestNet = false;

        public static bool enablePushNotifications = true; // Push notifications are disabled for now

        public static string walletFile = "wallet.ixi";

        public static string spixiUserFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Spixi");

        public static int encryptionRetryPasswordAttempts = 3;   // How many allowed attempts in the LaunchRetry page before throwing the user back to Launch Page

        // Read-only values
        public static readonly string aboutUrl = "https://www.spixi.io";

        public static readonly string pushServiceUrl = "https://ipn.ixian.io/v1";

        public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

        public static readonly int packetDataSize = 102400; // 100 Kb per packet for file transfers
        public static readonly long packetRequestTimeout = 60; // Time in seconds to re-request packets

        public static readonly string version = "spixi-0.4.5-dev"; // Spixi version

        // Default SPIXI settings
        public static bool defaultXamarinAnimations = false;

        // App-specific settings
        public static bool storeHistory = true;

        // Push notifications OneSignal AppID
        public static string oneSignalAppId = "44d96ce3-5d33-4e8b-997d-d1ad786b96a1";

        // Block height at which the current version of Spixi was generated
        // Useful for optimized block header sync
        public static ulong bakedBlockHeight = 1117064;

        // Block checksum (paired with bakedBlockHeight) at which the current version of Spixi was generated
        // Useful for optimized block header sync
        public static byte[] bakedBlockChecksum = Crypto.stringToHash("eba79772cddafa5aa3c51278771bcbb7105f285f8ba8b90bdf22b4d0c9b85e61ca1440aa7d29fd95252da228");
    }
}