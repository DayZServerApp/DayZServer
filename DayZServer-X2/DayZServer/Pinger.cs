using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net.NetworkInformation;

namespace DayZServer
{
    public class Pinger : INotifyPropertyChanged
    {
        private string speed;
        // Declare the event 
        public event PropertyChangedEventHandler PropertyChanged;

        public Pinger()
        {
        }

        public string this[string ip]
        {
            get { return new Ping().Send(ip).RoundtripTime.ToString() + "ms"; }
        }

        public Pinger(string value)
        {
            this.speed = value;
        }

        public string PingSpeed
        {
            get { return speed; }
            set
            {
                speed = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged("PingSpeed");
            }
        }

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string speed)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(speed));
            }
        }
    }
}