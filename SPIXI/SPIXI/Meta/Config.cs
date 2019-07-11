
namespace SPIXI.Meta
{
    public class Config
    {
        // Providing pre-defined values
        // Can be read from a file later, or read from the command line

        public static bool isTestNet = true;

        public static string walletFile = "spixi.wal";

        public static int encryptionRetryPasswordAttempts = 3;   // How many allowed attempts in the LaunchRetry page before throwing the user back to Launch Page

        // Read-only values
        public static readonly string aboutUrl = "https://ixian.io";

        public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

        public static readonly string version = "spixi-0.1.0-dev"; // Spixi version

        // Default SPIXI settings
        public static bool defaultXamarinAnimations = false;

        // App-specific settings
        public static bool storeHistory = true;

        private Config()
        {

        }
    }
}