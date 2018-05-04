using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
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

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for Help.xaml
    /// </summary>
    public partial class Help : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsHelp = new DataSet();
        int currentHelpRow = 0;
        string pageID = "";

        public Help()
        {
            InitializeComponent();
        }

        public Help(string helpPageID)
        {
            InitializeComponent();
            this.txtBlkBanner.Text += " for " + helpPageID;
            pageID = helpPageID;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadHelpTopics()
        {

            string sql = "SELECT A.*, B.UncontrolledFileLink AS Source FROM HelpTopics A, DocumentMaster B " +
                "WHERE A.DocumentID = B.ID AND A.PageID = '" + pageID + "' ORDER BY A.PageDescription";

            dsHelp = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgHelp, false, dsHelp);

            dgHelp.Columns[0].Visibility = Visibility.Hidden;
            dgHelp.Columns[2].Header = "Help Topic Description";
            dgHelp.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dgHelp.Columns[1].Visibility = Visibility.Hidden;
            dgHelp.Columns[3].Visibility = Visibility.Hidden;
            dgHelp.Columns[4].Visibility = Visibility.Hidden;

            tools.SelectDGGridRowByIndex(dgHelp, 0);

            if (dsHelp.Tables[0].Rows.Count == 0)
            {
                MessageBox.Show("There are currently no HELP documents available for this page.", "Notice");
                this.Close();
            }
        }

        private void ViewHelpDocuments()
        {
            if (dsHelp.Tables[0].Rows.Count > 0)
            {
                int sourceDocID = (int)dsHelp.Tables[0].Rows[currentHelpRow]["DocumentID"];
                string sourceHelpFile = dsHelp.Tables[0].Rows[currentHelpRow]["Source"].ToString();
                string sourceDocTitle = dsHelp.Tables[0].Rows[currentHelpRow]["PageDescription"].ToString();
                if (File.Exists(sourceHelpFile))
                {
                    tools.ViewPrintDocument(dsHelp, sourceDocID);
                    this.Close();
                }
                else { MessageBox.Show("The requested Help file cannot be found.", "Notice"); }
            }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            ViewHelpDocuments();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadHelpTopics();
        }

        private void dgHelp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentHelpRow = tools.GetGridRow(dgHelp);
        }

        private void dgHelp_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewHelpDocuments();
        }
    }
}
