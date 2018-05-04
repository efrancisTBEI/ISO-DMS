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
using System.Windows.Shapes;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for NotifyInBox.xaml
    /// </summary>
    public partial class NotifyInBox : Window
    {
        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        string currentUserName = "";
        string currentUser = "";

        int currentInboxRow = 0;

        DataSet dsInbox = new DataSet();

        public NotifyInBox()
        {
            InitializeComponent();
            currentUserName = Properties.Settings.Default.CurrentUsername.Replace(".", " ").ToUpper();
            currentUser = Properties.Settings.Default.CurrentUsername;
            txtBlkInBox.Text = "InBox for:  [ " + currentUserName + " ]";
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void LoadInbox(int row = 0)
        {
            dsInbox.Clear();

            string sql = "SELECT A.ID, A.MessageID, A.MessageViewed, A.MessageDate, A.Sender, A.Recipient, B.Title, A.Notes, " +
                "B.ControlledFileLink, B.UncontrolledFileLink, A.DocumentID FROM Inbox A, DocumentMaster B " +
                "WHERE A.DocumentID = B.ID AND Recipient = '" + currentUser + "' ORDER BY MessageID DESC";

            dsInbox = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgInbox, false, dsInbox, row, 7);

            dgInbox.Columns[0].Visibility = Visibility.Hidden;
            dgInbox.Columns[1].Visibility = Visibility.Hidden;
            dgInbox.Columns[5].Visibility = Visibility.Hidden;
            dgInbox.Columns[8].Visibility = Visibility.Hidden;
            dgInbox.Columns[9].Visibility = Visibility.Hidden;
            dgInbox.Columns[10].Visibility = Visibility.Hidden;
            dgInbox.Columns[2].Header = "Viewed";
            dgInbox.Columns[3].Header = "Date";
            dgInbox.Columns[6].Header = "Document";

            if (dsInbox.Tables[0].Rows.Count > 0)
            {
                cMenuInBox.Visibility = Visibility.Visible;
            }
            else
            {
                cMenuInBox.Visibility = Visibility.Hidden;
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadInbox();
        }

        private void dgInbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentInboxRow = tools.GetGridRow(dgInbox);
            if (dsInbox.Tables[0].Rows.Count > 0)
            {
                bool documentRead = (bool)dsInbox.Tables[0].Rows[currentInboxRow]["MessageViewed"];
                if (documentRead)
                {
                    mnuMarkDocument.Header = "Mark Highlighted Document as UNREAD";
                }
                else
                {
                    mnuMarkDocument.Header = "Mark Highlighted Document as READ";
                }
            }
        }

        private void ViewPrintDocument()
        {
            if (dsInbox.Tables[0].Rows.Count > 0)
            {

                // Start Acrobat and view the document.
                int docID = (int)dsInbox.Tables[0].Rows[currentInboxRow]["DocumentID"];
                string inDocument = dsInbox.Tables[0].Rows[currentInboxRow]["ControlledFileLink"].ToString();
                string outDocument = dsInbox.Tables[0].Rows[currentInboxRow]["UncontrolledFileLink"].ToString();
                tools.ViewPrintDocument(dsInbox, docID, inDocument, outDocument);

                // Now mark the document as viewed
                int row = currentInboxRow;
                int ID = (int)dsInbox.Tables[0].Rows[currentInboxRow]["ID"];
                string sql = "UPDATE Inbox SET MessageViewed = 1 WHERE ID = " + ID.ToString();
                tools.DBExecuteNonQuery(sql);
                LoadInbox(row);
            }
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintDocument();
        }

        private void mnuViewPrintDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintDocument();
        }

        private void dgInbox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void mnuMarkDocument_Click(object sender, RoutedEventArgs e)
        {
            bool readStatus = (mnuMarkDocument.Header.ToString().Contains("UNREAD")) ? false : true;
            MarkDocumentReadStatus(readStatus);
        }

        private void MarkDocumentReadStatus(bool markAsRead)
        {
            // Now mark the document as viewed
            int readStatus = (markAsRead) ? 1 : 0;

            int row = currentInboxRow;
            int ID = (int)dsInbox.Tables[0].Rows[currentInboxRow]["ID"];
            string sql = "UPDATE Inbox SET MessageViewed = " + readStatus.ToString() + " WHERE ID = " + ID.ToString();
            tools.DBExecuteNonQuery(sql);
            LoadInbox(row);
            currentInboxRow = row;
        }
    }
}
