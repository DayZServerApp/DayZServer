using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Segment;
using System.Reflection;
using System.Diagnostics;



namespace DayZ
{
   
        public class DZA
        {


            public string userName = Environment.UserName;

        static void Main(string[] args)
        {


        }


        public DZA()
        {
                    }
       

           

            public void runDayZ()
            {
                
                string timestamp = DateTime.Now.ToString();
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;
                Analytics.Initialize("1rPqfFBffSmEORGGzg5iDJFM6LC4UwOR");
                Analytics.Client.Identify(userName, new Segment.Model.Traits
            {    
                {"name", userName}, 
                {"date", timestamp},
                {"version", version}
            });
                Analytics.Client.Track(userName, "Started DayZServer");
            }


            public void serverJoin(string serverName, string FullIP_Address)
            {
               
                string timestamp = DateTime.Now.ToString();
                Analytics.Client.Track(userName, "Joined Server", new Segment.Model.Properties() {
                {"serverName", serverName}, 
                {"ip", FullIP_Address},
                {"date", timestamp}

});
            }

            public void exitSave(string startTime, string durration)
            {
                Analytics.Client.Track(userName, "Time In App", new Segment.Model.Properties() {
                {"durration", durration},
                {"starttime", startTime}

                

});
            }
        }

    
}

