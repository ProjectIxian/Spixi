using System;
using System.Collections.Generic;
using System.Text;


// Dummy Network server for IXICore
namespace DLT.Network
{
    public class NetworkServer
    {
        // Returns all the connected clients
        public static string[] getConnectedClients(bool useIncomingPort = false)
        {
            List<String> result = new List<String>();
            return result.ToArray();
        }
    }
}
