using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
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
    /// Interaction logic for ManageHelp.xaml
    /// </summary>
    public partial class ManageHelp : Page
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        ISO_DMS.Tools tools = new ISO_DMS.Tools();

        int currentAssignedHelpDocumentsRow = 0;
        int currentMasterHelpDocumentsRow = 0;

        DataSet dsAssignedHelpDocuments = new DataSet();
        DataSet dsMasterHelpDocuments = new DataSet();

        public ManageHelp()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAssignedHelpDocuments();
            LoadMasterHelpDocuments();
        }

        private void btnGoHome_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void LoadMasterHelpDocuments()
        {
            string sql = "SELECT * FROM DocumentMaster WHERE DepartmentID = " + tools.HelpDepartmentID.ToString() + 
                " AND ID NOT IN (SELECT DocumentID FROM HelpTopics) ORDER BY Title";
            dsMasterHelpDocuments = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgMasterHelpDocuments, false, dsMasterHelpDocuments, 0);

            if (dsMasterHelpDocuments.Tables[0].Rows.Count > 0)
            { cMenuMasterHelpDocuments.Visibility = Visibility.Visible; }
            else
            { cMenuMasterHelpDocuments.Visibility = Visibility.Hidden; }
        }

        private void LoadAssignedHelpDocuments(int row = 0)
        {
            string sql = "SELECT ID, PageID, DocumentID, PageDescription FROM HelpTopics ORDER BY PageID, PageDescription";
            dsAssignedHelpDocuments = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgAssignedHelpDocuments, false, dsAssignedHelpDocuments, 0);
            dgAssignedHelpDocuments.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private void dgMasterHelpDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentMasterHelpDocumentsRow = tools.GetGridRow(dgMasterHelpDocuments);
        }

        private void mnuItemAssignDocumentToApplicationPage_Click(object sender, RoutedEventArgs e)
        {

            string documentID = dsMasterHelpDocuments.Tables[0].Rows[currentMasterHelpDocumentsRow]["ID"].ToString();
            string pageDescription = dsMasterHelpDocuments.Tables[0].Rows[currentMasterHelpDocumentsRow]["Title"].ToString();
            string pageID = dsMasterHelpDocuments.Tables[0].Rows[currentMasterHelpDocumentsRow]["ISOTag"].ToString();
            string sql = "SELECT COUNT(*) FROM HelpTopics WHERE PageID = " + tools.chr39 + pageID + tools.chr39 +
                " AND DocumentID = " + documentID;

            int result = tools.DBExecuteScalar(sql);

            if (result == 0)
            {
                sql = "INSERT INTO HelpTopics (PageID, DocumentID, PageDescription) " +
                    "VALUES(" + tools.chr39 + pageID + tools.chr39 + "," + documentID + ", " +
                    tools.chr39 + pageDescription + tools.chr39 + ")";

                tools.DBExecuteNonQuery(sql);
                LoadAssignedHelpDocuments(0);
                LoadMasterHelpDocuments();
            }
            else
            {
                MessageBox.Show("This Help Document as already been assigned to the application.", "Notice");
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("ManageHelp");
            help.ShowDialog();
        }
    }
}
