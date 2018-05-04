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
    /// Interaction logic for NotifyOutBox.xaml
    /// </summary>
    public partial class NotifyOutBox : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        string currentUserName = "";
        string currentUser = "";

        int currentAvailableUsersRow = 0;
        int currentSelectedUsersRow = 0;
        int currentQueuedDocumentsRow = 0;

        DataSet dsAvailableUsers = new DataSet();
        DataSet dsSelectedUsers = new DataSet();
        DataSet dsQueuedDocuments = new DataSet();

        public NotifyOutBox()
        {
            InitializeComponent();
            currentUser = Properties.Settings.Default.CurrentUsername;
            currentUserName = Properties.Settings.Default.CurrentUsername.Replace(".", " ").ToUpper();
            txtBlkOutBox.Text = "OutBox for:  [ " + currentUserName + " ]";
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void LoadAvailableUsers()
        {
            //string sql = "SELECT UserName FROM Users WHERE UserName <> '" + currentUser + "' ORDER BY UserName";
            string sql = "SELECT UserName FROM Users ORDER BY UserName";
            dsAvailableUsers = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgAvailableUsers, false, dsAvailableUsers, 0, 0);
            dgAvailableUsers.HeadersVisibility = DataGridHeadersVisibility.None;
        }

        private void LoadSelectedUsers()
        {
            string sql = "SELECT * FROM OutboxQueuedRecipients WHERE Sender = '" + currentUser + "' ORDER BY Recipient";
            dsSelectedUsers = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgSelectedUsers, false, dsSelectedUsers, 0, 2);
            dgSelectedUsers.HeadersVisibility = DataGridHeadersVisibility.None;
            dgSelectedUsers.Columns[0].Visibility = Visibility.Hidden;
            dgSelectedUsers.Columns[1].Visibility = Visibility.Hidden;

            btnSend.IsEnabled = dsSelectedUsers.Tables[0].Rows.Count > 0;
        }

        private void LoadQueuedDocuments()
        {
            string sql = "SELECT A.*, B.Title, B.ControlledFileLink, B.UncontrolledFileLInk FROM OutboxQueuedDocuments A, DocumentMaster B " +
                "WHERE A.DocumentID = B.ID ORDER BY B.Title";
            dsQueuedDocuments = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgQueuedDocuments, false, dsQueuedDocuments, 0, 3);
            dgQueuedDocuments.HeadersVisibility = DataGridHeadersVisibility.None;
            dgQueuedDocuments.Columns[0].Visibility = Visibility.Hidden;
            dgQueuedDocuments.Columns[1].Visibility = Visibility.Hidden;
            dgQueuedDocuments.Columns[2].Visibility = Visibility.Hidden;
            dgQueuedDocuments.Columns[4].Visibility = Visibility.Hidden;
            dgQueuedDocuments.Columns[5].Visibility = Visibility.Hidden;

            if (dsQueuedDocuments.Tables[0].Rows.Count > 0)
            {
                cMenuQueuedDocuments.Visibility = Visibility.Visible;
            }
            else
            {
                cMenuQueuedDocuments.Visibility = Visibility.Hidden;
            }

            btnSend.IsEnabled = (dsQueuedDocuments.Tables[0].Rows.Count > 0 && dsSelectedUsers.Tables[0].Rows.Count > 0);
        }

        private void AddRecipient()
        {
            if (dsAvailableUsers.Tables[0].Rows.Count > 0)
            {
                string sender = currentUser;
                string recipient = dsAvailableUsers.Tables[0].Rows[currentAvailableUsersRow]["UserName"].ToString();
                string sql = "SELECT COUNT(*) FROM OutboxQueuedRecipients WHERE Recipient = '" + recipient + "'";

                int results = tools.DBExecuteScalar(sql);

                if (results == 0)
                {
                    sql = "INSERT INTO OutboxQueuedRecipients (Sender, Recipient) " +
                        "VALUES('" + sender + "','" + recipient + "')";
                    tools.DBExecuteNonQuery(sql);
                    LoadSelectedUsers();

                    int row = 0;

                    for (int x = 0; x <= dsSelectedUsers.Tables[0].Rows.Count - 1; x++)
                    {
                        row = x;
                        if (recipient == dsSelectedUsers.Tables[0].Rows[x]["Recipient"].ToString()) break;
                    }

                    tools.SelectDGGridRowByIndex(dgSelectedUsers, row);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadAvailableUsers();
            LoadSelectedUsers();
            LoadQueuedDocuments();
        }

        private void dgAvailableUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentAvailableUsersRow = tools.GetGridRow(dgAvailableUsers);
        }

        private void dgSelectedUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentSelectedUsersRow = tools.GetGridRow(dgSelectedUsers);
        }

        private void dgQueuedDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentQueuedDocumentsRow = tools.GetGridRow(dgQueuedDocuments);
        }

        private void btnAddRecipient_Click(object sender, RoutedEventArgs e)
        {
            AddRecipient();
        }

        private void dgAvailableUsers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddRecipient();
        }

        private void btnRemoveRecipient_Click(object sender, RoutedEventArgs e)
        {
            RemoveRecipient();
        }

        private void RemoveRecipient()
        {
            if (dsSelectedUsers.Tables[0].Rows.Count > 0)
            {
                int row = currentSelectedUsersRow;
                int ID = (int)dsSelectedUsers.Tables[0].Rows[currentSelectedUsersRow]["ID"];
                string sql = "DELETE FROM OutboxQueuedRecipients WHERE ID = " + ID.ToString();
                tools.DBExecuteNonQuery(sql);
                LoadSelectedUsers();

                if (row > dsSelectedUsers.Tables[0].Rows.Count - 1) { row -= 1; }

                tools.SelectDGGridRowByIndex(dgSelectedUsers, row);
            }
        }

        private void dgSelectedUsers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RemoveRecipient();
        }

        private void ViewPrintDocument()
        {
            if (dsQueuedDocuments.Tables[0].Rows.Count > 0)
            {

                int docID = (int)dsQueuedDocuments.Tables[0].Rows[currentQueuedDocumentsRow]["DocumentID"];
                string inDocument = dsQueuedDocuments.Tables[0].Rows[currentQueuedDocumentsRow]["ControlledFileLink"].ToString();
                string outDocument = dsQueuedDocuments.Tables[0].Rows[currentQueuedDocumentsRow]["UncontrolledFileLink"].ToString();
                tools.ViewPrintDocument(dsQueuedDocuments, docID, inDocument, outDocument);
            }
        }

        private void mnuViewPrintDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintDocument();
        }

        private void RemoveQueuedDocument()
        {
            if (dsQueuedDocuments.Tables[0].Rows.Count > 0)
            {
                int row = currentQueuedDocumentsRow;
                int ID = (int)dsQueuedDocuments.Tables[0].Rows[currentQueuedDocumentsRow]["ID"];
                string sql = "DELETE FROM OutboxQueuedDocuments WHERE ID = " + ID.ToString();
                tools.DBExecuteNonQuery(sql);
                LoadQueuedDocuments();

                if (row > dsQueuedDocuments.Tables[0].Rows.Count - 1) { row -= 1; }

                tools.SelectDGGridRowByIndex(dgQueuedDocuments, row);
            }
        }

        private void mnuItemRemoveDocument_Click(object sender, RoutedEventArgs e)
        {
            RemoveQueuedDocument();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendDocuments();
        }

        private void SendDocuments()
        {
            bool canSend = (dsQueuedDocuments.Tables[0].Rows.Count > 0 && dsSelectedUsers.Tables[0].Rows.Count > 0);
            if (canSend)
            {
                string sender = Properties.Settings.Default.CurrentUsername;

                for (int x = 0; x <= dsSelectedUsers.Tables[0].Rows.Count -1; x++)
                {
                    string recipient = dsSelectedUsers.Tables[0].Rows[x]["Recipient"].ToString();
                    int messageID = tools.DBExecuteScalar("SELECT ISNULL(MAX(MessageID),0) + 1 AS MessageID FROM Inbox");

                    for (int y = 0; y <= dsQueuedDocuments.Tables[0].Rows.Count - 1; y++)
                    {
                        int docID = (int)dsQueuedDocuments.Tables[0].Rows[y]["DocumentID"];

                        string sql = "INSERT INTO Inbox (MessageID, Sender, Recipient, DocumentID) " +
                            "VALUES(" + messageID.ToString() + ",'" + sender + "','" + recipient + "'," + docID.ToString() + ")";

                        tools.DBExecuteNonQuery(sql);

                        sql = "DELETE FROM OutboxQueuedRecipients WHERE Sender = '" + sender + "'";
                        tools.DBExecuteNonQuery(sql);

                        sql = "DELETE FROM OutboxQueuedDocuments WHERE Sender = '" + sender + "'";
                        tools.DBExecuteNonQuery(sql);
                    }
                }
                MessageBox.Show("Your documents have successully been sent to the selected recipients!");
                this.Close();
            }

        }
    }
}
