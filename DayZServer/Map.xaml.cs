using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace DayZServer
{
    /// <summary>
    /// Interaction logic for Map.xaml
    /// </summary>
    public partial class Map : Window
    {


        Model3DGroup map;

        Model3D terrain;
        public Map()
        {
            InitializeComponent();
            ModelImporter importer = new ModelImporter();

            map = new Model3DGroup();

            //load the files
            terrain = importer.Load(@"Map/map.obj");
        }
    }
}
