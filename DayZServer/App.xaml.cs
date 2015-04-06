using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using DayZ;
using System.Net;
using System.Xml;
using System.Web;


namespace Steam
{
    

    
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string IPAddress;
        private string startTime;
        private void App_Startup(object sender, StartupEventArgs e)
        {
            //string IPAddress = string.Empty;
            //String strHostName = System.Web.HttpContext.Current.Request.UserHostAddress.ToString();
            //IPAddress = System.Net.Dns.GetHostAddresses(strHostName).GetValue(0).ToString();
            //DataTable location = new DataTable();
            //location = GetLocation(IPAddress);
            startTime = DateTime.Now.ToString(@"hh\:mm\:ss");
        }

        private DataTable GetLocation(string ipaddress)
        {
            WebRequest rssReq = WebRequest.Create("http://freegeoip.appspot.com/xml/" + ipaddress);
            WebProxy px = new WebProxy("http://freegeoip.appspot.com/xml/" + ipaddress, true);
            rssReq.Proxy = px;
            rssReq.Timeout = 2000;
            try
            {
                WebResponse rep = rssReq.GetResponse();
                XmlTextReader xtr = new XmlTextReader(rep.GetResponseStream());
                DataSet ds = new DataSet();
                ds.ReadXml(xtr);
                return ds.Tables[0];
            }
            catch
            {
                return null;
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            string exitTime = DateTime.Now.ToString(@"hh\:mm\:ss");
            TimeSpan duration = DateTime.Parse(exitTime).Subtract(DateTime.Parse(startTime));
            DZA dza = new DZA();
            string totaltime = duration.ToString(@"hh\:mm\:ss");
            dza.exitSave(startTime.ToString(), totaltime);
        }
    }




}
