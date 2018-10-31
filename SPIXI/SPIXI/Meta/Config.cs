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

            public static string walletFile = "wallet.dat";

            // Store the device id in a cache for reuse in later instances
            public static string device_id = Guid.NewGuid().ToString();

            // Read-only values
            public static readonly string aboutUrl = "https://ixian.io";

            public static readonly int nodeVersion = 4;
            public static readonly int networkClientReconnectInterval = 10 * 1000; // Time in milliseconds

            // Default SPIXI settings

            // App-specific settings
            public static bool storeHistory = true;


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