using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for SecurityLog.xaml
    /// </summary>
    public partial class SecurityLog : Page
    {

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        public SecurityLog()
        {
            InitializeComponent();
        }


        private void LoadSecurityLog()
        {
            string sql = "SELECT * FROM SecurityLog ORDER BY LogDate DESC";
            DataSet ds = tools.DBCreateDataSet(sql);

            tools.ConfigureDataGridOptions(dgSecurityLog,false,ds);
            tools.SelectDGGridRowByIndex(dgSecurityLog, 0);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSecurityLog();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = TimeSpan.FromMilliseconds(10000);
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            LoadSecurityLog();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
