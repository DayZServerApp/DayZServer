using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DayZServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        DispatcherTimer _timer;
        TimeSpan _time;
        TimeSpan _timeset;

        public MainWindow()
        {
            InitializeComponent();
            _time = TimeSpan.FromSeconds(10);
            _timer = new DispatcherTimer(new TimeSpan(0, 0, 0, 0), DispatcherPriority.Background, delegate
            {
                this.dateText.Text = DateTime.UtcNow.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture);
                //this.dateText.Text =_time.ToString(@"ss");
                //if (_time == TimeSpan.Zero)
                //{
                //    _timer.Stop();
                //    _time = TimeSpan.FromSeconds(10000);
                //    _timer.Start();
                //}
                //_time = _time.Add(TimeSpan.FromSeconds(-1));
            }, this.Dispatcher);
            _timer.Start();
        }
    }
}
