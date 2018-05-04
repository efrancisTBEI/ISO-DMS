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
    /// Interaction logic for DocumentNotes.xaml
    /// </summary>
    public partial class DocumentNotes : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        int currentDocumentNotesRow = 0;
        int documentID = 0;
        string documentTitle = "";

        DataSet dsDocumentNotes = new DataSet();

        public DocumentNotes(int docID, string docTitle)
        {
            InitializeComponent();

            txtBlkDocumentTitle.Text = docTitle;
            documentID = docID;
            documentTitle = docTitle;
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentDocumentNotesRow = tools.GetGridRow(dgDocumentNotes);
        }

        private void LoadDocumentNotes(int docID)
        {
            dsDocumentNotes.Clear();

            string sql = "SELECT * FROM DocumentNotes WHERE DocumentID = " + docID.ToString() +
                " ORDER BY NotesCreateDate DESC";

            dsDocumentNotes = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgDocumentNotes, false, dsDocumentNotes, 0);

            dgDocumentNotes.Columns[1].Visibility = Visibility.Hidden;
            dgDocumentNotes.Columns[2].Header = "Date Created";
            dgDocumentNotes.Columns[3].Header = "Date Modified";
            dgDocumentNotes.Columns[4].Header = "User";
            dgDocumentNotes.Columns[5].Header = "Notes";
            dgDocumentNotes.Columns[5].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDocumentNotes(documentID);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("DocumentNotes");
            help.ShowDialog();
        }

        private void btnAddNote_Click(object sender, RoutedEventArgs e)
        {
            DocumentNotesWriter dNotesWriter = new DocumentNotesWriter(0,documentID, documentTitle);
            dNotesWriter.ShowDialog();
            LoadDocumentNotes(documentID);
        }

        private void btnEditNote_Click(object sender, RoutedEventArgs e)
        {
            if (dsDocumentNotes.Tables[0].Rows.Count > 0)
            {
                string documentCreator = dsDocumentNotes.Tables[0].Rows[currentDocumentNotesRow]["UserName"].ToString();
                string currentUser = Properties.Settings.Default.CurrentUsername;

                if (documentCreator == currentUser)
                {
                    int ID = (int)dsDocumentNotes.Tables[0].Rows[currentDocumentNotesRow]["ID"];
                    string documentText = dsDocumentNotes.Tables[0].Rows[currentDocumentNotesRow]["NotesComment"].ToString();
                    DocumentNotesWriter dNotesWriter = new DocumentNotesWriter(ID, documentID, documentTitle, documentText);
                    dNotesWriter.ShowDialog();
                    LoadDocumentNotes(documentID);
                }
                else
                {
                    MessageBox.Show("You are not allowed to edit a note that you did not create!", "Notice");
                }
            }
        }

        private void btnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (dsDocumentNotes.Tables[0].Rows.Count > 0)
            {

                string documentCreator = dsDocumentNotes.Tables[0].Rows[currentDocumentNotesRow]["UserName"].ToString();
                string currentUser = Properties.Settings.Default.CurrentUsername;

                if (documentCreator == currentUser)
                {
                    int ID = (int)dsDocumentNotes.Tables[0].Rows[currentDocumentNotesRow]["ID"];
                    if (MessageBox.Show("Delete this note?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        string sql = "DELETE FROM DocumentNotes WHERE ID = " + ID.ToString();
                        tools.DBExecuteNonQuery(sql);
                        LoadDocumentNotes(documentID);
                    }
                }
                else
                {
                    MessageBox.Show("You are not allowed to delete a note that you did not create!", "Notice");
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

    }
}
