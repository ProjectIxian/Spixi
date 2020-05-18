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
        // Note: Always round last block height to 1000 and subtract 1 (i.e. if last block height is 1000, the correct value is 999)
        public static ulong bakedBlockHeight = 1132999;

        // Block checksum (paired with bakedBlockHeight) of bakedBlockHeight
        // Useful for optimized block header sync
        public static byte[] bakedBlockChecksum = Crypto.stringToHash("cfb531a4d9131dd61caedf8052b9d481c73fa9894c0d949f56db1e2deba2e53b9dadd2d6de5a025d847e838e");


        // Temporary variables for bh sync recovery
        // Note: Always round last block height to 1000 and subtract 1 (i.e. if last block height is 1000, the correct value is 999)
        public static ulong bakedRecoveryBlockHeight = 999999;
        public static byte[] bakedRecoveryBlockChecksum = Crypto.stringToHash("fa9d2126ecb78648b45e8d4bc382503c27563e7815bfaf6d32ef4b95bdd4041b7a631fc559fa007fb6af7e74");
    }
}