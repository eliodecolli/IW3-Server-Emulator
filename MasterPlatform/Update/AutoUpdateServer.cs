using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace MasterPlatform.Update
{
    public class AutoUpdateServer
    {
        UdpClient udp = new UdpClient(28960);

        void xrun()
        {
            while(true)
            {
                IPEndPoint clientIP = new IPEndPoint(IPAddress.Any, 28960);
                byte[] data = udp.Receive(ref clientIP);
                MemoryStream memsr = new MemoryStream(data);
                BinaryReader reader = new BinaryReader(memsr);
                Log2.Info("Header_Int: " + reader.ReadInt32().ToString());
                Log2.Info("Body: " + reader.ReadString());
            }
        }

        public void Start()
        {
            Log2.Debug("Update server is now running!");
            new Thread(xrun).Start();
        }
    }
}
