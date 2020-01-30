using IXICore;
using IXICore.Meta;
using Newtonsoft.Json;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace SPIXI
{
    class OfflinePushMessages
    {

        public static bool sendPushMessage(StreamMessage msg)
        {
            string receiver = Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient);
            string data = HttpUtility.UrlEncode(Convert.ToBase64String(msg.getBytes()));

            Friend f = FriendList.getFriend(msg.recipient);
            if (f == null)
                return false;
            
            string URI = String.Format("{0}/push.php", Config.pushServiceUrl);
            string parameters = String.Format("tag={0}&data={1}", receiver, data);

            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string htmlCode = client.UploadString(URI, parameters);
                if (htmlCode.Equals("OK"))
                    return true;
            }
            return false;
        }

        public static bool fetchPushMessages()
        {
            try
            {
                string URI = String.Format("{0}/fetch.php", Config.pushServiceUrl);
                string unique_uri = String.Format("{0}/uniqueid.php", Config.pushServiceUrl);

                string receiver = Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.getPrimaryAddress());
        
                WebClient uclient = new WebClient();
                byte[] checksum = Convert.FromBase64String(uclient.DownloadString(unique_uri));

                byte[] sig = CryptoManager.lib.getSignature(checksum, Node.walletStorage.getPrimaryPrivateKey());

                using (WebClient client = new WebClient())
                {
                    string url = String.Format("{0}?tag={1}&sig={2}", URI, receiver, HttpUtility.UrlEncode(Convert.ToBase64String(sig)));
                    string htmlCode = client.DownloadString(url);
                    Logging.info(htmlCode);

                    List<string> jsonResponse = JsonConvert.DeserializeObject<List<string>>(htmlCode);

                    foreach (string str in jsonResponse)
                    {
                        try
                        {
                            byte[] data = Convert.FromBase64String(str);
                            StreamProcessor.receiveData(data, null);
                        }
                        catch(Exception e)
                        {
                            Logging.error(string.Format("Exception occured in fetchPushMessages while parsing the json response. {0}", e));
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Exception occured in fetchPushMessages. {0}", e));
                return false;
            }

            return true;
        }
    }
}
