using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DLT
{
    namespace Meta
    {

        public class Config
        {
            // Providing pre-defined values
            // Can be read from a file later, or read from the command line
            public static int serverPort = 10111;
            public static int apiPort = 8001;
            public static string publicServerIP = "127.0.0.1";

            public static string walletFile = "ixian.wal";

            // Store the device id in a cache for reuse in later instances
            public static string device_id = Guid.NewGuid().ToString();

            // Read-only values
            public static readonly string aboutUrl = "https://ixian.io";

            public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

            public static readonly string version = "spixi-0.1.0"; // Spixi version
            public static bool isTestNet = true; // Testnet designator

            // Default SPIXI settings
            public static bool defaultXamarinAnimations = false;

            // App-specific settings
            public static bool storeHistory = true;


            // internal
            public static bool changePass = false;
            public static int forceTimeOffset = int.MaxValue;

            


            private static Config singletonInstance;
            private Config()
            {

            }

            public static Config singleton
            {
                get
                {
                    if (singletonInstance == null)
                    {
                        singletonInstance = new Config();
                    }
                    return singletonInstance;
                }
            }

            public static void readFromCommandLine(string[] args)
            {

            }

        }

    }
}