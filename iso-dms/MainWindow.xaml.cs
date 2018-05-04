using System;
using System.Collections.Generic;
using System.Deployment.Application;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();
        IniFile ini = new IniFile(@"C:\Temp\MTTS.ini");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string versionInfo = ApplicationDeployment.CurrentDeployment.CurrentVersion.Major.ToString() + "." +
                    ApplicationDeployment.CurrentDeployment.CurrentVersion.MajorRevision.ToString() + "." +
                    ApplicationDeployment.CurrentDeployment.CurrentVersion.MinorRevision.ToString();

                ini.WriteValue("Program", "VersionInfo", versionInfo);
            }
            catch
            {
            }

            this.Title = "ISO-DMS v" + ini.ReadValue("Program","VersionInfo").ToString() + " - Current User:  [" + buck.GetCurrentUserName().ToUpper().Replace(".", " ") + "]";

            this.WindowState = WindowState.Maximized;
            this.frame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
            this.frame.Height = this.Height;
            this.frame.Width = this.Width;

            this.frame.Content = (new MainPage());

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //tools.WriteSecurityLogEntry(0, tools.logEvent_LoggedOut, "N/A");
        }
    }
}
