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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for DepartmentModulesMenu.xaml
    /// </summary>
    public partial class DepartmentModulesMenu : Page
    {
        public DepartmentModulesMenu()
        {
            InitializeComponent();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("DepartmentModulesMenu");
            help.ShowDialog();
        }
    }
}
