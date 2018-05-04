using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.Odbc;
using System.Data;
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
    /// Interaction logic for SOPTagToJobCode.xaml
    /// </summary>
    public partial class SOPTagToJobCode : Page
    {
        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsOdysseyJobCodes = new DataSet();
        DataSet dsMasterDocuments = new DataSet();
        DataSet dsLinkedDocuments = new DataSet();
        DataSet dsSQLJobCodes = new DataSet();
        DataSet dsISODepartments = new DataSet();

        public Char chr39 = Convert.ToChar(39);

        int CurrentJobCodeRow = 0;
        int CurrentLinkedDocumentsRow = 0;
        int CurrentISODepartmentsRow = 0;
        int CurrentMasterDocumentsRow = 0;

        string employeeLookupID = "";
        string supervisorLookupID = "";
        string jobCodeLookUp = "";

        IniFile ini = new IniFile(@"C:\Temp\MTTS.ini");

        string strSQL = "SELECT * FROM DocumentMaster WHERE ISOType = 'QC' OR ISOType = 'SOP' ORDER BY ISOTag";

        public SOPTagToJobCode()
        {
            InitializeComponent();
        }

        public SOPTagToJobCode(string employeeID, string supervisorID)
        {
            InitializeComponent();
            employeeLookupID = employeeID;
            supervisorLookupID = supervisorID;
        }

        public SOPTagToJobCode(string employeeID, string supervisorID, string jobCode)
        {
            InitializeComponent();
            employeeLookupID = employeeID;
            supervisorLookupID = supervisorID;
            jobCodeLookUp = jobCode;
        }

        private void GoHome()
        {
            if (ini.ReadValue("Navigation", "NextPage") == "ISO-SOP.xaml")
            {
                ini.WriteValue("UserInfo", "SOPEmployeeID", employeeLookupID);
                NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
            }
            else
            {
                ini.WriteValue("UserInfo", "SOPEmployeeID", "");
                NavigationService.GoBack();
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            GoHome();
        }

        private void dgMasterDocuments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddLinkedDocument();
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void dgMasterDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentMasterDocumentsRow = tools.GetGridRow(dgMasterDocuments);
        }

        private void dgJobCodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentJobCodeRow = tools.GetGridRow(this.dgJobCodes);
            if (dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString().Length >= 4)
            {
                dgISODepartments.IsEnabled = dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString().Substring(0, 4) != "DEPT";
                btnAddISODepartments.IsEnabled = dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString().Substring(0, 4) != "DEPT";
            }
            else
            {
                dgISODepartments.IsEnabled = true;
                btnAddISODepartments.IsEnabled = true;
            }
            LoadLinkedDocuments(0);
        }

        private void dgLinkedDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentLinkedDocumentsRow = tools.GetGridRow(dgLinkedDocuments);
        }

        private void LoadISODepartments()
        {
            buck.DBOpenOdysseyDatabase(false);

            dsISODepartments.Clear();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM SOPJobCodes WHERE JobCode LIKE 'DEPT%' ORDER BY JobCode";

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dsISODepartments);

            dgISODepartments.ItemsSource = dsISODepartments.Tables[0].DefaultView;
            dgISODepartments.Columns[0].Visibility = Visibility.Hidden;

            buck.DBCloseDatabase();

            tools.ConfigureDataGridOptions(dgISODepartments);

            dgISODepartments.CanUserSortColumns = false;

            dgISODepartments.Columns[1].Header = "Job Code";
            dgISODepartments.Columns[2].Header = "Description";
            dgISODepartments.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(dgISODepartments, 0);

        }

        private void LoadJobCodes(int row = 0)
        {
            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmdOdysseyJobCodes = new OdbcCommand();

            cmdOdysseyJobCodes.Connection = buck.cnOdyssey;
            cmdOdysseyJobCodes.CommandType = CommandType.Text;
            cmdOdysseyJobCodes.CommandText = @"SELECT DISTINCT WorkPhone AS JobCode FROM Employees WHERE CompanyID = 'buck' and active = 1 
                AND substring(WorkPhone,1, 1) NOT IN ('1', '2', '3', '4', '5', '6', '7', '8', '9', '0', ' ') ORDER BY JobCode";


            dsOdysseyJobCodes.Clear();

            OdbcDataAdapter daOdysseyJobCodes = new OdbcDataAdapter(cmdOdysseyJobCodes);
            daOdysseyJobCodes.Fill(dsOdysseyJobCodes);

            tools.SelectDGGridRowByIndex(this.dgJobCodes, row);

            buck.DBCloseOdysseyDatabase();

            tools.DBOpenSQLDB();

            SqlCommand cmdSQLJobCodes = new SqlCommand();
            SqlDataAdapter daSQLJobCodes = new SqlDataAdapter();

            for (int x = 0; x < dsOdysseyJobCodes.Tables[0].Rows.Count - 1; x++)
            {
                // Check to see if the Job Code is new or already exists
                cmdSQLJobCodes.Connection = tools.cnSQLDB;
                cmdSQLJobCodes.CommandType = CommandType.Text;
                cmdSQLJobCodes.CommandText = "SELECT * FROM SOPJobCodes WHERE JobCode = '" + dsOdysseyJobCodes.Tables[0].Rows[x]["JobCode"].ToString() + "'";

                dsSQLJobCodes.Clear();
                daSQLJobCodes.SelectCommand = cmdSQLJobCodes;
                daSQLJobCodes.Fill(dsSQLJobCodes);

                // Not found, so add a new record
                if (dsSQLJobCodes.Tables[0].Rows.Count == 0)
                {
                    cmdSQLJobCodes.CommandText = "INSERT INTO SOPJobCodes (JobCode) VALUES('" + dsOdysseyJobCodes.Tables[0].Rows[x]["JobCode"].ToString() + "')";
                    cmdSQLJobCodes.ExecuteNonQuery();
                }

            }

            dsSQLJobCodes.Clear();

            if (jobCodeLookUp.Length == 0)
            { cmdSQLJobCodes.CommandText = "SELECT * FROM SOPJobCodes ORDER BY JobCode"; }
            else
            { cmdSQLJobCodes.CommandText = "SELECT * FROM SOPJobCodes WHERE JobCode = '" + jobCodeLookUp + "' ORDER BY JobCode"; }

            daSQLJobCodes.Fill(dsSQLJobCodes);

            dgJobCodes.ItemsSource = dsSQLJobCodes.Tables[0].DefaultView;
            buck.DBCloseDatabase();

            tools.ConfigureDataGridOptions(dgJobCodes);

            dgJobCodes.CanUserSortColumns = false;

            dgJobCodes.Columns[0].Visibility = Visibility.Hidden;
            dgJobCodes.Columns[1].Header = "Job Code";
            dgJobCodes.Columns[2].Header = "Description";
            dgJobCodes.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(dgJobCodes, row);

            if (dsSQLJobCodes.Tables[0].Rows.Count == 0)
            {
                cMenuJobCodes.Visibility = Visibility.Hidden;
            }
            else
            {
                cMenuJobCodes.Visibility = Visibility.Visible;
            }

        }

        private void LoadLinkedDocuments(int row = 0, string documentTitle = "")
        {
            dsLinkedDocuments.Clear();
            tools.DBOpenSQLDB();

            SqlCommand cmdLinkedDocuments = new SqlCommand();
            cmdLinkedDocuments.Connection = tools.cnSQLDB;
            cmdLinkedDocuments.CommandType = CommandType.Text;
            cmdLinkedDocuments.CommandText = "SELECT A.*,B.ISOType, B.ISOTag, B.IsoRevision, B.Title  FROM SOPJobCodeLinks A, "
                + "DocumentMaster B WHERE A.Document_ID = B.ID AND A.JobCode = '"
                + dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString() + "' ORDER BY B.ISOType, B.Title";

            SqlDataAdapter daLinkedDocuments = new SqlDataAdapter(cmdLinkedDocuments);
            daLinkedDocuments.Fill(dsLinkedDocuments);


            dgLinkedDocuments.ItemsSource = dsLinkedDocuments.Tables[0].DefaultView;

            // If adding a new record to the linked documents table then highlight the last row.
            if (row == -1)
            {
                // row = dsLinkedDocuments.Tables[0].Rows.Count - 1;
                for (int x = 0; x <= dsLinkedDocuments.Tables[0].Rows.Count - 1; x += 1)
                {
                    if (dsLinkedDocuments.Tables[0].Rows[x]["Title"].ToString() == documentTitle)
                    {
                        row = x;
                        break;
                    }
                }
            }

            tools.ConfigureDataGridOptions(dgLinkedDocuments);

            dgLinkedDocuments.CanUserSortColumns = false;

            dgLinkedDocuments.Columns[0].Visibility = Visibility.Hidden;
            dgLinkedDocuments.Columns[1].Visibility = Visibility.Hidden;
            dgLinkedDocuments.Columns[2].Visibility = Visibility.Hidden;
            dgLinkedDocuments.Columns[3].Header = "ISO\nType";
            dgLinkedDocuments.Columns[4].Header = "ISO\nTag";
            dgLinkedDocuments.Columns[5].Header = "ISO\nRevision";
            dgLinkedDocuments.Columns[6].Header = "Document Title";
            dgLinkedDocuments.Columns[6].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(this.dgLinkedDocuments, row);
            CurrentLinkedDocumentsRow = row;

            // Disable the Context Menu if no records are returned.
            if (dsLinkedDocuments.Tables[0].Rows.Count > 0)
            { cMenuLinkedJobCodes.Visibility = Visibility.Visible; }
            else
            { cMenuLinkedJobCodes.Visibility = Visibility.Hidden; }

        }

        private void GetMasterDocs()
        {
            // For SOP Management only documents with ISOTypes of SOP and QC are loaded.
            // The strSQL variable is declared and filled with the appropriate code
            // at the top of this document.
            tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments, 0, strSQL, true, false, true);
            dgMasterDocuments.Columns[dgMasterDocuments.Columns.Count - 1].Visibility = Visibility.Hidden;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // See if a Job Code is being passed by the Employee Job Assignments form.
            //string jobCodeLookup = Properties.Settings.Default.SOPJobCodeLookUp.ToString();
            LoadJobCodes();
            LoadLinkedDocuments();

            GetMasterDocs();

            dgMasterDocuments.Columns[0].Visibility = Visibility.Hidden;
            LoadISODepartments();

            if (jobCodeLookUp.Length > 0)
            {
                Properties.Settings.Default.SOPJobCodeLookUp = "";
                Properties.Settings.Default.Save();

                int row = 0;
                for (int x = 0; x <= dsSQLJobCodes.Tables[0].Rows.Count-1; x++)
                {
                    row = x;
                    if (dsSQLJobCodes.Tables[0].Rows[x]["JobCode"].ToString() == jobCodeLookUp)
                    { break; }
                }

                tools.SelectDGGridRowByIndex(dgJobCodes, row);
                CurrentJobCodeRow = row;
                LoadLinkedDocuments();
            }

            txtSearch.Focus();
        }

        private void btnAddLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            AddLinkedDocument();
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void AddISODepartmentDocumentsToJobCode()
        {
            string ISOJobCode = dsISODepartments.Tables[0].Rows[CurrentISODepartmentsRow]["JobCode"].ToString();

            //string documentTitle = "";

            //bool recordsAdded = false;
            bool recordsNotAdded = false;

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandText = "SELECT DISTINCT JobCode, Document_ID FROM SOPJobCodeLinks WHERE JobCode = " + chr39 + ISOJobCode + chr39;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            string JobCode = dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString();

            for (int x = 0; x < ds.Tables[0].Rows.Count - 1; x++)
            {
                int docID = (int)ds.Tables[0].Rows[x]["Document_ID"];

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT COUNT(*) FROM SOPJobCodeLinks WHERE Document_ID = " + docID + " AND JobCode = " + chr39 + JobCode + chr39;

                int returnValue = (int)cmd.ExecuteScalar();

                if (returnValue == 0)
                {
                    // recordsAdded = true;
                    cmd.CommandText = "INSERT INTO SOPJobCodeLinks (JobCode, Document_ID) VALUES('" + JobCode + "'," + docID + ")";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    recordsNotAdded = true;
                }
            }

            buck.DBCloseDatabase();

            LoadLinkedDocuments(0);

            if (recordsNotAdded)
            {
                MessageBox.Show("One or more documents was not added because it was already linked.", "Notice");
            }
        }

        private void btnAddISODepartments_Click(object sender, RoutedEventArgs e)
        {
            AddISODepartmentDocumentsToJobCode();
        }



        private void AddLinkedDocument()
        {
            bool recordsAdded = false;
            bool recordsNotAdded = false;

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;

            string documentTitle = "";

            for (int x = 0; x < dgMasterDocuments.SelectedItems.Count; x++)
            {
                DataRowView dr = (DataRowView)dgMasterDocuments.SelectedItems[x];

                // Check to see if the current document is already linked.  If not, add it.
                int docID = Convert.ToInt32(dr.Row.ItemArray[0]);
                documentTitle = dr.Row.ItemArray[5].ToString();
                string JobCode = dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT COUNT(*) FROM SOPJobCodeLinks WHERE Document_ID = " + docID + " AND JobCode = " + Convert.ToChar(39) + JobCode + Convert.ToChar(39);

                int returnValue = (int)cmd.ExecuteScalar();
                if (returnValue == 0)
                {
                    recordsAdded = true;
                    cmd.CommandText = "INSERT INTO SOPJobCodeLinks (JobCode, Document_ID) VALUES('" + dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCode"].ToString() + "'," + docID + ")";
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    recordsNotAdded = true;
                }
            }

            buck.DBCloseDatabase();

            if (recordsAdded)
            {
                LoadLinkedDocuments(-1, documentTitle);
            }
            else
            {
                LoadLinkedDocuments(CurrentLinkedDocumentsRow);
            }

            if (recordsNotAdded)
            {
                MessageBox.Show("One or more documents was not added because it was already linked.", "Notice");
            }

            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void RemoveLinkedDocument()
        {
            if (dsLinkedDocuments.Tables[0].Rows.Count > 0)
            {
                int row = CurrentLinkedDocumentsRow;

                int id = (int)dsLinkedDocuments.Tables[0].Rows[CurrentLinkedDocumentsRow]["ID"];

                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM SOPJobCodeLinks WHERE ID = " + id;
                cmd.ExecuteNonQuery();

                if (row >= dsLinkedDocuments.Tables[0].Rows.Count - 1)
                {
                    if (row > 0) row -= 1;
                }

                LoadLinkedDocuments(row);
                tools.SelectDGGridRowByIndex(dgLinkedDocuments, row);
                CurrentLinkedDocumentsRow = row;
                txtSearch.Text = "";
                txtSearch.Focus();
            }
        }
        private void btnRemoveLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            RemoveLinkedDocument();
        }

        private void SearchDocuments()
        {
            if (dsMasterDocuments.Tables[0].Rows.Count > 0)
            {
                SearchDocuments SearchDocuments = new SearchDocuments();
                SearchDocuments.ShowDialog();

                if (SearchDocuments.SQLText.Length > 0)
                {
                    tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments, 0, SearchDocuments.SQLText, false, true);
                }
                else
                {
                    //tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments, CurrentMasterDocumentsRow, strSQL, false, true);
                    GetMasterDocs();
                }
            }

            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void btnSearchDocuments_Click(object sender, RoutedEventArgs e)
        {
            SearchDocuments();
        }

        private void ViewPrintMasterDocument()
        {
            if (dsMasterDocuments.Tables[0].Rows.Count > 0)
            {

                int cellValue = 0;
                foreach (DataRowView row in dgMasterDocuments.SelectedItems)
                {
                    cellValue = (int)row.Row.ItemArray[0];
                }

                tools.ViewPrintDocument(dsMasterDocuments, cellValue);

                txtSearch.Text = "";
                txtSearch.Focus();
            }
        }

        private void btnViewPrint_Click(object sender, RoutedEventArgs e)
        {
            if (dsMasterDocuments.Tables[0].Rows.Count > 0)
            {

                int cellValue = 0;
                foreach (DataRowView row in dgMasterDocuments.SelectedItems)
                {
                    cellValue = (int)row.Row.ItemArray[0];
                }

                tools.ViewPrintDocument(dsMasterDocuments, cellValue);

                txtSearch.Text = "";
                txtSearch.Focus();

            }

        }

        private void ViewPrintLinkedDocument()
        {
            if (dsLinkedDocuments.Tables[0].Rows.Count > 0)
            {
                int cellValue = 0;
                foreach (DataRowView row in dgLinkedDocuments.SelectedItems)
                {
                    cellValue = (int)row.Row.ItemArray[2];
                }

                tools.ViewPrintDocument(dsLinkedDocuments, cellValue);

                txtSearch.Text = "";
                txtSearch.Focus();
            }
        }

        private void btnViewPrintLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintLinkedDocument();
        }

        private void ClearSearchFilters()
        {
            dgMasterDocuments.Items.SortDescriptions.Clear();
            dgMasterDocuments.Items.Refresh();

            foreach (DataGridColumn column in dgMasterDocuments.Columns)
            {
                column.SortDirection = null;
            }

            Properties.Settings.Default.documentSearchInProgress = false;

            //tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments, CurrentMasterDocumentsRow, "", false, true);
            GetMasterDocs();

            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchFilters();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.ToString().ToUpper().Contains("QC"))
            {
                txtSearch.Text = txtSearch.Text.ToString().Substring(2);
            }

            string strSQLText = "SELECT DocumentMaster.*, Departments.DepartmentName FROM DocumentMaster LEFT OUTER JOIN Departments " +
                    "ON DocumentMaster.DepartmentID = Departments.ID ";

            //strSQL += "WHERE DocumentMaster.Title LIKE " + chr39 + "%" + txtSearch.Text + "%" + chr39 + " ";
            strSQLText += "WHERE DocumentMaster.ISOTag LIKE " + chr39 + txtSearch.Text + "%" + chr39 + " ";
            strSQLText += "ORDER BY DocumentMaster.ISOTag";

            if (txtSearch.Text.Length > 0)
            { tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments, 0, strSQLText, false, true); }
            else
            { GetMasterDocs(); }

            txtSearch.Focus();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            txtSearch.Background = Brushes.Yellow;
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            txtSearch.Background = Brushes.White;
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key  == Key.Return)
            {
                AddLinkedDocument();
                txtSearch.Text = "";
                txtSearch.Focus();
            }
        }

        private void dgJobCodes_MouseUp(object sender, MouseButtonEventArgs e)
        {
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void dgLinkedDocuments_MouseUp(object sender, MouseButtonEventArgs e)
        {
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void dgISODepartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentISODepartmentsRow = tools.GetGridRow(dgISODepartments);
        }

        private void mnuItemJobCodes_Click(object sender, RoutedEventArgs e)
        {
            string JobCodeDescription = dsSQLJobCodes.Tables[0].Rows[CurrentJobCodeRow]["JobCodeDescription"].ToString();
            InputBoxMultiLine ml = new InputBoxMultiLine("Edit Job Code Description:", JobCodeDescription);
            ml.ShowDialog();

            if (ml.itemText.Length > 0)
            {
                int curRow = CurrentJobCodeRow;
                int jobCodeID = (int)dsSQLJobCodes.Tables[0].Rows[curRow]["ID"];

                tools.DBOpenSQLDB();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "UPDATE SOPJobCodes SET JobCodeDescription = " + chr39 + ml.itemText + chr39 + " WHERE ID = " + jobCodeID.ToString();
                cmd.ExecuteNonQuery();

                buck.DBCloseDatabase();

                LoadJobCodes(curRow);
                buck.DoEvents();
                CurrentJobCodeRow = curRow;
                LoadLinkedDocuments();
                txtSearch.Text = "";
                txtSearch.Focus();
            }   
        }

        private void txtEmployee_GotFocus(object sender, RoutedEventArgs e)
        {
            txtEmployee.Background = Brushes.Yellow;
        }

        private void txtEmployee_LostFocus(object sender, RoutedEventArgs e)
        {
            txtEmployee.Background = Brushes.White;
        }

        private void txtEmployee_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtEmployee.Text = txtEmployee.Text.ToUpper();
            int row = 0;
            bool found = false;

            if (txtEmployee.Text.Length > 0)
            {
                for (int x = 0; x <= dsSQLJobCodes.Tables[0].Rows.Count - 1; x++)
                {
                    row = x;
                    if (txtEmployee.Text.Length <= dsSQLJobCodes.Tables[0].Rows[x]["JobCodeDescription"].ToString().Length)
                    {
                        if (dsSQLJobCodes.Tables[0].Rows[x]["JobCodeDescription"].ToString().Substring(0, txtEmployee.Text.Length).ToUpper() == txtEmployee.Text.ToUpper())
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            { row = 0; }

            CurrentJobCodeRow = row;
            tools.SelectDGGridRowByIndex(dgJobCodes, CurrentJobCodeRow);

            //string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
            LoadLinkedDocuments();

            txtEmployee.SelectionStart = txtEmployee.Text.Length;
            txtEmployee.Focus();

        }

        private void mnuItemAddDocumentToJobCode_Click(object sender, RoutedEventArgs e)
        {
            AddLinkedDocument();
            txtSearch.Text = "";
            txtSearch.Focus();
        }

        private void mnuItemViewPrintMasterDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintMasterDocument();
        }

        private void mnuItemViewPrintLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintLinkedDocument();
        }

        private void mnuItemRemoveLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            RemoveLinkedDocument();
        }

        private void mnuItemClearSearchFilter_Click(object sender, RoutedEventArgs e)
        {
            ClearSearchFilters();
        }

        private void mnuItemSearchDocuments_Click(object sender, RoutedEventArgs e)
        {
            SearchDocuments();
        }

        private void mnuItemAddDepartmentDocumentsToJobCode_Click(object sender, RoutedEventArgs e)
        {
            AddISODepartmentDocumentsToJobCode();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F4:
                    AddLinkedDocument();
                    break;
                case Key.F12:
                    GoHome();
                    break;
                case Key.Home:
                    txtSearch.Text = "";
                    txtSearch.Focus();
                    break;
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("SOPTagToJobCode");
            help.ShowDialog();
        }
    }
}
