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
    public class OfflinePushMessages
    {
        public static long lastUpdate = 0;
        private static int cooldownPeriod = 60; // cooldown period in seconds

        private static string pushNotificationAuthKey = null;

        private static long nonce = Clock.getTimestamp();

        public static bool sendPushMessage(StreamMessage msg, bool push)
        {
            string receiver = Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient);
            string sender = Base58Check.Base58CheckEncoding.EncodePlain(msg.sender);
            string data = HttpUtility.UrlEncode(Convert.ToBase64String(msg.getBytes()));

            string pub_key = "";

            if (msg.id.Length == 1 && msg.id[0] == 1)
            {
                pub_key = HttpUtility.UrlEncode(Convert.ToBase64String(Node.walletStorage.getPrimaryPublicKey()));
            }


            Friend f = FriendList.getFriend(msg.recipient);
            if (f == null)
                return true; // return true to skip sending this message and remove it from the queue

            string URI = String.Format("{0}/push.php", Config.pushServiceUrl);
            string parameters = String.Format("tag={0}&data={1}&pk={2}&push={3}&fa={4}", receiver, data, pub_key, push, sender);

            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string htmlCode = client.UploadString(URI, parameters);
                    if (htmlCode.Equals("OK"))
                    {
                        return true;
                    }
                }catch(Exception e)
                {
                    Logging.error("Exception occured in sendPushMessage: " + e);
                }
            }
            return false;
        }

        public static bool fetchPushMessages(bool force = false)
        {
            if(force == false && lastUpdate + cooldownPeriod > Clock.getTimestamp())
            {
                return false;
            }

            lastUpdate = Clock.getTimestamp();

            if (pushNotificationAuthKey == null)
            {
                if (!registerWithPushNotificationServer())
                {
                    return false;
                }
            }

            try
            {
                string receiver = Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.getPrimaryAddress());

                nonce++;

                byte[] sig = Crypto.sha512(UTF8Encoding.UTF8.GetBytes(nonce + pushNotificationAuthKey));

                using (WebClient client = new WebClient())
                {
                    string url = String.Format("{0}/fetch.php?tag={1}&nonce={2}&sig={3}", Config.pushServiceUrl, receiver, nonce, Crypto.hashToString(sig));
                    string htmlCode = client.DownloadString(url);

                    if (htmlCode.StartsWith("ERROR"))
                    {
                        if(htmlCode.StartsWith("ERROR: Nonce too low "))
                        {
                            nonce = Int32.Parse(htmlCode.Substring("ERROR: Nonce too low ".Length));
                        }
                        return false;
                    }

                    if (htmlCode == "UNREGISTERED")
                    {
                        pushNotificationAuthKey = null;
                        registerWithPushNotificationServer();
                        return false;
                    }

                    List<string[]> jsonResponse = JsonConvert.DeserializeObject<List<string[]>>(htmlCode);

                    if(jsonResponse != null && jsonResponse.Count > 0)
                    {
                        lastUpdate = 0; // If data was available, fetch it again without cooldown
                    }

                    foreach (string[] str in jsonResponse)
                    {
                        try
                        {
                            byte[] data = Convert.FromBase64String(str[1]);
                            if (str[2] != "")
                            {
                                byte[] pk = Convert.FromBase64String(str[2]);
                                Friend f = FriendList.getFriend(new Address(pk).address);
                                if (f != null && f.publicKey == null)
                                {
                                    f.publicKey = pk;
                                    FriendList.saveToStorage();
                                }
                            }
                            StreamProcessor.receiveData(data, null);
                        }
                        catch(Exception e)
                        {
                            Logging.error("Exception occured in fetchPushMessages while parsing the json response: {0}", e);
                        }

                        try
                        {
                            nonce++;
                            sig = Crypto.sha512(UTF8Encoding.UTF8.GetBytes(nonce + pushNotificationAuthKey));

                            string id = str[0];
                            url = String.Format("{0}/remove.php?id={1}&nonce={2}&sig={3}", Config.pushServiceUrl, id, nonce, Crypto.hashToString(sig));
                            htmlCode = client.DownloadString(url);
                        }
                        catch (Exception e)
                        {
                            Logging.error("Exception occured in fetchPushMessages while removing the message from server: {0}", e);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in fetchPushMessages: {0}", e);
                return false;
            }

            return true;
        }

        private static bool registerWithPushNotificationServer()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    nonce++;

                    byte[] sig = CryptoManager.lib.getSignature(UTF8Encoding.UTF8.GetBytes(nonce.ToString()), Node.walletStorage.getPrimaryPrivateKey());

                    string url = String.Format("{0}/register.php", Config.pushServiceUrl);
                    string parameters = String.Format("pk={0}&nonce={1}&sig={2}", Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.getPrimaryPublicKey()), nonce, Crypto.hashToString(sig));

                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string htmlCode = client.UploadString(url, parameters);

                    if (htmlCode.StartsWith("ERROR"))
                    {
                        if (htmlCode.StartsWith("ERROR: Nonce too low "))
                        {
                            nonce = Int32.Parse(htmlCode.Substring("ERROR: Nonce too low ".Length));
                        }
                        return false;
                    }

                    List<string> jsonResponse = JsonConvert.DeserializeObject<List<string>>(htmlCode);

                    if(jsonResponse[0] != "")
                    {
                        pushNotificationAuthKey = jsonResponse[0];
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error("Exception occured in registerWithPushNotificationServer: {0}", e);
                return false;
            }

            return true;
        }
    }
}
