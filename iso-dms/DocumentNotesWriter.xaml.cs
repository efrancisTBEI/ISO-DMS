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
    /// Interaction logic for DocumentNotesWriter.xaml
    /// </summary>
    public partial class DocumentNotesWriter : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        int ID = 0;
        int documentID = 0;
        string documentText = "";
        bool editMode = false;

        public DocumentNotesWriter(int internalDocID, int docID, string docTitle, string docText = "")
        {
            InitializeComponent();
            ID = internalDocID;
            documentID = docID;
            documentText = docText;
            this.txtBlkDocumentTitle.Text = docTitle;

            if (docText.Length > 0)
            {
                editMode = true;
                this.txtNotes.Text = docText;
            }

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtNotes.Focus();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveNote(editMode);
            this.Close();
        }

        private void SaveNote(bool blnInsert = false)
        {
            tools.DBOpenSQLDB();
            
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@DocumentID", documentID);
            cmd.Parameters.AddWithValue("@UserName", Properties.Settings.Default.CurrentUsername);
            cmd.Parameters.AddWithValue("@NotesComment", txtNotes.Text);

            if (!editMode)
            {
                if (txtNotes.Text.Length > 0)
                {
                    cmd.CommandText = "INSERT INTO DocumentNotes (DocumentID,UserName,NotesCreateDate,NotesModifiedDate, NotesComment) " +
                        "VALUES(@DocumentID,@UserName,GetDate(),GetDate(),@NotesComment)";
                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                if (txtNotes.Text != documentText)
                {
                    cmd.CommandText = "UPDATE DocumentNotes SET NotesComment = @NotesComment, NotesModifiedDate = GetDate() WHERE ID = " + ID.ToString();
                    cmd.ExecuteNonQuery();
                }
            }

            buck.DBCloseDatabase();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
