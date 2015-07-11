// Please take note that this software is speed-coded and it is not yet optimized to perform as it should. //


// Author: Elio Decolli.
// Version: 0.1 (Supports both COD2 and COD4)



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MasterPlatform.Update;
using MasterPlatform.Dedi;
using System.Net;

namespace MasterPlatform
{

    class Program
    {

        static string GetHeader()
        {
            string title = "IW3 Server - OpenWorld Labs.";
            string web = "Visit us at: openworld.icyboards.com";
            string stars = new string('*', web.Length + 4);
            int pdist = stars.Length - title.Length - 3;
            string space = new string(' ', pdist);
            StringBuilder strb = new StringBuilder();
            strb.AppendLine(stars);
            strb.AppendLine("* " + title + space + '*');
            strb.AppendLine("* " + web + " *");
            strb.AppendLine(stars);
            return strb.ToString();
        }

        static void Main(string[] args)
        {
            Console.Title = "IW3 Master Server";
            Console.BackgroundColor = ConsoleColor.White;
            Console.Clear();
#if BETA
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("WARNING THIS VERSION IS PRETTY BUGGY!");
#endif
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(GetHeader());
            Log2.Initialize("Log2_Server.txt", LogLevel.All, false);

            ChanllegeServer cSer = new ChanllegeServer();
            cSer.Start();
            WelcomeServer wcmSer = new WelcomeServer();
            wcmSer.Start();
            while(true)
            {
                Log2.WriteAway();
            }
        }
    }


}
