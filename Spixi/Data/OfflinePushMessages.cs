using IXICore;
using IXICore.Meta;
using Newtonsoft.Json;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.Net.Http;
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
            string receiver = msg.recipient.ToString();
            string sender = msg.sender.ToString();
            string data = HttpUtility.UrlEncode(Convert.ToBase64String(msg.getBytes()));

            string pub_key = "";

            if (msg.id.Length == 1 && msg.id[0] == 1)
            {
                pub_key = HttpUtility.UrlEncode(Convert.ToBase64String(IxianHandler.getWalletStorage().getPrimaryPublicKey()));
            }


            Friend f = FriendList.getFriend(msg.recipient);
            if (f == null)
                return true; // return true to skip sending this message and remove it from the queue

            string url = string.Format("{0}/push.php", Config.pushServiceUrl);
            string parameters = string.Format("tag={0}&data={1}&pk={2}&push={3}&fa={4}", receiver, data, pub_key, push, sender);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpContent httpContent = new StringContent(parameters, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = client.PostAsync(url, httpContent).Result;
                    string body = response.Content.ReadAsStringAsync().Result;
                    if (body.Equals("OK"))
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
                string receiver = IxianHandler.getWalletStorage().getPrimaryAddress().ToString();

                nonce++;

                byte[] sig = Crypto.sha512(Encoding.UTF8.GetBytes(nonce + pushNotificationAuthKey));

                using (HttpClient client = new HttpClient())
                {
                    string url = string.Format("{0}/fetch.php?tag={1}&nonce={2}&sig={3}", Config.pushServiceUrl, receiver, nonce, Crypto.hashToString(sig));
                    string body = client.GetStringAsync(url).Result;

                    if (body.StartsWith("ERROR"))
                    {
                        if(body.StartsWith("ERROR: Nonce too low "))
                        {
                            nonce = int.Parse(body.Substring("ERROR: Nonce too low ".Length));
                        }
                        return false;
                    }

                    if (body == "UNREGISTERED")
                    {
                        pushNotificationAuthKey = null;
                        registerWithPushNotificationServer();
                        return false;
                    }

                    List<string[]> jsonResponse = JsonConvert.DeserializeObject<List<string[]>>(body);

                    if(jsonResponse != null && jsonResponse.Count > 0)
                    {
                        lastUpdate = 0; // If data was available, fetch it again without cooldown
                    }

                    foreach (string[] str in jsonResponse)
                    {
                        try
                        {
                            if(str[1] != "")
                            {
                                byte[] data = Convert.FromBase64String(str[1]);
                                if (str[2] != "")
                                {
                                    byte[] pk = Convert.FromBase64String(str[2]);
                                    Friend f = FriendList.getFriend(new Address(pk));
                                    if (f != null && pk != null)
                                    {
                                        f.setPublicKey(pk);
                                    }
                                }
                                StreamProcessor.receiveData(data, null);
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.error("Exception occured in fetchPushMessages while parsing the json response: {0}", e);
                        }

                        try
                        {
                            nonce++;
                            sig = Crypto.sha512(Encoding.UTF8.GetBytes(nonce + pushNotificationAuthKey));

                            string id = str[0];
                            url = string.Format("{0}/remove.php?id={1}&nonce={2}&sig={3}", Config.pushServiceUrl, id, nonce, Crypto.hashToString(sig));
                            body = client.GetStringAsync(url).Result;
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
                using (HttpClient client = new HttpClient())
                {
                    nonce++;

                    byte[] sig = CryptoManager.lib.getSignature(Encoding.UTF8.GetBytes(nonce.ToString()), IxianHandler.getWalletStorage().getPrimaryPrivateKey());

                    string url = string.Format("{0}/register.php", Config.pushServiceUrl);
                    string parameters = string.Format("pk={0}&nonce={1}&sig={2}", Base58Check.Base58CheckEncoding.EncodePlain(IxianHandler.getWalletStorage().getPrimaryPublicKey()), nonce, Crypto.hashToString(sig));

                    HttpContent httpContent = new StringContent(parameters, Encoding.UTF8, "application/x-www-form-urlencoded");
                    var response = client.PostAsync(url, httpContent).Result;
                    string body = response.Content.ReadAsStringAsync().Result;

                    if (body.StartsWith("ERROR"))
                    {
                        if (body.StartsWith("ERROR: Nonce too low "))
                        {
                            nonce = int.Parse(body.Substring("ERROR: Nonce too low ".Length));
                        }
                        return false;
                    }

                    List<string> jsonResponse = JsonConvert.DeserializeObject<List<string>>(body);

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
