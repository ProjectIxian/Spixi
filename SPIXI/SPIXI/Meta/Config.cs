
using System.IO;

namespace SPIXI.Meta
{
    public class Config
    {
        // Providing pre-defined values
        // Can be read from a file later, or read from the command line

        public static bool isTestNet = false;

        public static string walletFile = "wallet.ixi";

        public static string spixiUserFolder = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "Spixi");

        public static int encryptionRetryPasswordAttempts = 3;   // How many allowed attempts in the LaunchRetry page before throwing the user back to Launch Page

        // Read-only values
        public static readonly string aboutUrl = "https://www.spixi.io";

        public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

        public static readonly int packetDataSize = 102400; // 100 Kb per packet for file transfers
        public static readonly long packetRequestTimeout = 30; // Time in seconds to re-request packets

        public static readonly string version = "spixi-0.2.0"; // Spixi version

        // Default SPIXI settings
        public static bool defaultXamarinAnimations = false;

        // App-specific settings
        public static bool storeHistory = true;

        private Config()
        {

        }
    }
}