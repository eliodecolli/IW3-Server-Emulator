using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MasterPlatform.Dedi
{
    public class DedicatedClient
    {
        public Dictionary<string, string> Configuration = new Dictionary<string, string>();
        public IPEndPoint IP;

        public DedicatedClient(string source, IPEndPoint ip)
        {
            string[] parts = source.Split('\\');
            List<string> keys = new List<string>();
            List<string> vals = new List<string>();
            for(int i = 1; i < parts.Length; i++)
            {
                if (i % 2 == 1)
                {
                    keys.Add(parts[i]);
                }
                else
                    vals.Add(parts[i]);
            }
            if (keys.Count == vals.Count)
            {
                for (int i = 0; i < keys.Count; i++)
                {
                    Configuration.Add(keys[i], vals[i]);
                }
            }
            else
                Log2.Error("Couldn't properly parse client info.");
            IP = ip;
            IP.Port = (int)ChanllegeServer.SwapShort(BitConverter.GetBytes((short)IP.Port));
        }

        public void Dump()
        {
            StringBuilder strb = new StringBuilder();
            foreach (var v in Configuration)
                strb.AppendLine(v.Key + ":" + v.Value);
            string f = strb.ToString();
            File.WriteAllText("client.txt", f);
        }
    }

    public class ChanllegeServer
    {

        public static short SwapShort(byte[] s)
        {
            byte[] arr = new byte[2];
            for (int i = 0; i < 2; i++)
            {
                // Some hardcoded shit
                if (i == 0)
                    arr[i] = s[i + 1];
                if (i == 1)
                    arr[i] = s[i - 1];
            }
            return BitConverter.ToInt16(arr, 0);
        }

        UdpClient udp = new UdpClient(20810);
        List<DedicatedClient> OnlineServers = new List<DedicatedClient>();

        void Done(IAsyncResult state)
        {
            Log2.Info("Sending message to client! [" + state.AsyncState.ToString() + "]");
        }

        byte[] IPToBytes(IPAddress ip)
        {
            byte[] data = new byte[4];
            string[] s = ip.ToString().Split('.');
            for (int i = 0; i < 4; i++)
                data[i] = byte.Parse(s[i]);
            return data;
        }

        byte[] GetShort(short number)
        {
            byte[] data = new byte[2];
            data[1] = (byte)(number >> 8);
            data[0] = (byte)(number & 255);
            return data;
        }

        int SendChallenge(IPEndPoint ip, UdpClient udp)
        {
            int c =  new Random().Next();
            MemoryStream getChng = new MemoryStream();
            BinaryWriter w = new BinaryWriter(getChng);
            w.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            w.Write(string.Format("getchallenge {0}",c));
            w.Flush();
            byte[] data = getChng.ToArray();
            udp.Send(data, data.Length, ip);
            Log2.Info("Sending challange data to " + ip.ToString() + "...");
            return c;
        }

        void xrun()
        {
            while (true)
            {
                IPEndPoint clientIP = new IPEndPoint(IPAddress.Any, 00000);
                byte[] data = udp.Receive(ref clientIP);
                MemoryStream memsr = new MemoryStream(data);
                Log2.Info(Encoding.ASCII.GetString(data));
                BinaryReader reader = new BinaryReader(memsr);
                if(data.Length > 4)
                {
                    reader.ReadInt32(); // Clear the header...
                    string type = Encoding.ASCII.GetString(reader.ReadBytes(data.Length - 4));
                    if (type.Contains("heartbeat"))
                    {
                        if(!type.Contains("flatline"))  //We still on...
                        {
                            int a = SendChallenge(clientIP, udp);
                            MemoryStream gsts = new MemoryStream();
                            BinaryWriter w = new BinaryWriter(gsts);
                            w.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                            w.Write("getstatus " + a.ToString());
                            w.Flush();
                            byte[] bdata = gsts.ToArray();
                            udp.BeginSend(bdata, bdata.Length, clientIP, new AsyncCallback(Done), "GT-STS");
                        }
                        else
                        {
                            var cs = from cl in OnlineServers where cl.IP.ToString() == clientIP.ToString() select cl;
                            if (cs.Count() > 0)
                            {
                                Log2.Info("Client [" + cs.First().Configuration["sv_hostname"] + "] has been disconnected!");
                                OnlineServers.Remove(cs.First());  // Server removed!
                            }
                        }
                    }
                    if (type.Contains("statusResponse"))
                    {
                        var cs = from cl in OnlineServers where cl.IP.ToString() == clientIP.ToString() select cl;
                        if (cs.Count() > 0)
                        {
                            int index = OnlineServers.IndexOf(cs.First());
                            OnlineServers[index] = new DedicatedClient(type, clientIP);
                            Log2.Info("Client [" + OnlineServers[index].Configuration["sv_hostname"] + "] has been updated.");
                        }
                        else
                        {
                            DedicatedClient dc = new DedicatedClient(type, clientIP);
                            OnlineServers.Add(dc);
                            Log2.Info("Connected client [" + dc.Configuration["sv_hostname"] + "].");
                        }
                      
                    }
                    if(type.Contains("challengeResponse"))  // Do we really wanna mess up with this ?
                    {
                        /*long chng = long.Parse(type.Split(' ')[1]);
                        MemoryStream memsr2 = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(memsr2);
                        writer.Write(new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF });  //Header
                        writer.Write("getstatus");
                        writer.Write(new byte[] { 0x02, 0x2D });
                        writer.Write(chng);
                        writer.Write(0x00);
                        writer.Flush();
                        byte[] fres = memsr2.ToArray();
                        memsr2.Dispose(); //Stack saver?
                        udp.BeginSend(fres, fres.Length, clientIP, new AsyncCallback(Done), "STS-REQ-" + chng.ToString());  // done with the chanllage shit!*/
                    }
                    if(type.Contains("getservers"))
                    {
                        // Protocol Check
                        // 6 - Call of Duty 4
                        // 118 - Call of Duty 2
                        if(type.Split(' ')[1] == "6")
                        {
                            MemoryStream back = new MemoryStream();
                            BinaryWriter wr = new BinaryWriter(back);
                            wr.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                            wr.Write(new byte[] { 0x67, 0x65, 0x74, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x73, 0x52, 0x65, 0x73, 0x70, 0x6F, 0x6E, 0x73, 0x65 });
                            //wr.Write(new byte[] { 0x0A, 0x00 });  // Ermm.. magic number ?
                            wr.Write((byte)0x5C);
                            if(OnlineServers.Count > 0)
                            {
                                foreach (var cl in OnlineServers)
                                {
                                    byte[] ip = cl.IP.Address.GetAddressBytes();
                                    wr.Write(ip);
                                    wr.Write((short)cl.IP.Port);
                                }
                            }
                            wr.Write((byte)0x5C);
                            wr.Write(new byte[] { 0x45, 0x4f, 0x66 });
                            wr.Flush();
                            byte[] servs = back.ToArray();
                            udp.BeginSend(servs, servs.Length, clientIP, new AsyncCallback(Done), "GT-SERVERS");
                        }
                    }
                    if(type.Contains("getIpAuthorize"))
                    {
                        string[] parts = type.Split(' ');
                        MemoryStream memsr2 = new MemoryStream();
                        BinaryWriter writer = new BinaryWriter(memsr2);
                        writer.Write(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
                        writer.Write("ipAuthorize ");
                        writer.Write(parts[1] + " ");
                        writer.Write("accept KEY_IS_GOOD");
                        writer.Flush();
                        byte[] final = memsr2.ToArray();
                        udp.BeginSend(final, final.Length, clientIP, new AsyncCallback(Done), "IP-ACCEPT");
                    }
                }
                else
                {
                    Log2.Data("Unkown packet?");
                }
            }
        }

        public void Start()
        {
            Log2.Debug("Challenge server is now running!");;
            new Thread(xrun).Start();
        }
    }
}
