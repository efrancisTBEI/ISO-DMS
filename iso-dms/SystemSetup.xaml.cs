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
    /// Interaction logic for SystemSetup.xaml
    /// </summary>
    public partial class SystemSetup : Page
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        public SystemSetup()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new EmployeeJobAssignments());
        }

        private void btnSecurityLog_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SecurityLog());
        }

        private void btnSOPTagToJobCode_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SOPTagToJobCode());
        }

        private void btnCreateUDCategories_Click(object sender, RoutedEventArgs e)
        {
            ManageUDCategories ManageUDC = new ManageUDCategories();

            ManageUDC.ShowInTaskbar = false;
            ManageUDC.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ManageUDC.ShowDialog();

        }

        private void btnEditUsers_Click(object sender, RoutedEventArgs e)
        {
            EditUsers editUsers = new EditUsers();

            editUsers.ShowInTaskbar = false;
            editUsers.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            editUsers.ShowDialog();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tools.DBOpenSQLDB();

            DataSet ds = new DataSet();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT ISOType, COUNT(ISOType) AS Count " +
                "FROM DocumentMaster WHERE (ISOType <> '' " +
                "AND IsDeprecated = 0) " +
                "GROUP BY ISOType ORDER BY ISOType";

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            dgDocumentCounts.ItemsSource = ds.Tables[0].DefaultView;
            tools.ConfigureDataGridOptions(dgDocumentCounts);

            dgDocumentCounts.Columns[0].Header = "Document Type";
            dgDocumentCounts.Columns[1].Header = "Count";

                buck.DBCloseDatabase();

        }

        private void btnSOPTagToUDCategories_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show(tools.getNextDMSFileName());
        }

        private void btnManageHelpDocuments_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ManageHelp());
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("SystemSetup");
            help.ShowDialog();
        }

        private void btnManageDepartments_Click(object sender, RoutedEventArgs e)
        {
            ManageDepartments mp = new ManageDepartments();
            mp.ShowDialog();
        }
    }
}
