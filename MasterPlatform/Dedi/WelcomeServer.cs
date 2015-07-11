using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace MasterPlatform.Dedi
{
    public class WelcomeServer
    {
        UdpClient udp = new UdpClient(20700);
        void xrun()
        {
            while (true)
            {
                IPEndPoint clientIP = new IPEndPoint(IPAddress.Any, 20700);
                byte[] data = udp.Receive(ref clientIP);
                string str = Encoding.ASCII.GetString(data);
                Log2.Data(str);
                udp.Send(data, data.Length, clientIP);
            }
        }

        public void Start()
        {
            Log2.Debug("Welcome server is now running!");
            new Thread(xrun).Start();
        }
    }
}
