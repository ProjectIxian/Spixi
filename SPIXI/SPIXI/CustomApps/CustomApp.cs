using IXICore;
using IXICore.Meta;
using System;

namespace SPIXI.CustomApps
{
    class CustomApp
    {
        public string id = "";
        public string publisher = "";
        public string name = "";
        public string version = "";
        public byte[] publicKey = null;
        public byte[] signature = null;

        public CustomApp(string[] app_info)
        {
            foreach(string command in app_info)
            {
                int cmd_sep_index = command.IndexOf('=');
                if(cmd_sep_index == -1)
                {
                    continue;
                }

                string key = command.Substring(0, cmd_sep_index).Trim(new char[] { ' ', '\t', '\r', '\n' });
                string value = command.Substring(cmd_sep_index + 1).Trim(new char[] { ' ', '\t', '\r', '\n' });

                if (key.StartsWith(";"))
                {
                    continue;
                }

                Logging.info("Processing config parameter '" + key + "' = '" + value + "'");
                int caVersion = 0;
                switch(key)
                {
                    case "caVersion":
                        caVersion = Int32.Parse(value);
                        break;

                    case "id":
                        id = value;
                        break;

                    case "publisher":
                        publisher = value;
                        break;

                    case "name":
                        name = value;
                        break;

                    case "version":
                        version = value;
                        break;

                    case "publicKey":
                        publicKey = Crypto.stringToHash(value);
                        break;

                    case "signature":
                        signature = Crypto.stringToHash(value);
                        break;
                }
            }
        }
    }
}
