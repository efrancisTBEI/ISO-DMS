using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for EmployeeJobAssignments.xaml
    /// </summary>
    public partial class EmployeeJobAssignments : Page
    {

        IniFile ini = new IniFile(@"C:\Temp\MTTS.ini");

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        Char chr39 = Convert.ToChar(39);

        DataSet dsEmployees = new DataSet();
        DataSet dsSQLJobCodes = new DataSet();
        DataSet dsLinkedJobCodes = new DataSet();
        DataSet dsOdysseyJobCodes = new DataSet();

        int CurrentRowEmployees = 0;
        int CurrentRowSOPJobCodes = 0;
        int CurrentRowLinkedJobCodes = 0;

        string employeeLookupID = "";
        string supervisorLookupID = "";

        public EmployeeJobAssignments()
        {
            InitializeComponent();
        }

        public EmployeeJobAssignments(string employeeID, string supervisorID)
        {
            InitializeComponent();
            employeeLookupID = employeeID;
            supervisorLookupID = supervisorID;
            ini.WriteValue("Navigation", "NextPage", "ISO-SOP.xaml");

        }

        private void button_Click(object sender, RoutedEventArgs e)
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

        private void LoadLinkedJobCodes(string EmployeeID,int row = 0,string jobCode = "",string primaryJobCode = "")
        {
            dsLinkedJobCodes.Clear();

            tools.DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT A.*,B.JobCodeDescription FROM EmployeeJobs A, SOPJobCodes B " +
                "WHERE A.JobCode = B.JobCode AND A.EmployeeID = '" + EmployeeID + "' ORDER BY SortOrder, JobCode";

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dsLinkedJobCodes);

            dgEmployeeJobs.ItemsSource = dsLinkedJobCodes.Tables[0].DefaultView;

            tools.ConfigureDataGridOptions(dgEmployeeJobs);
            dgEmployeeJobs.CanUserSortColumns = false;
            dgEmployeeJobs.Columns[0].Visibility = Visibility.Hidden;
            dgEmployeeJobs.Columns[1].Visibility = Visibility.Hidden;
            dgEmployeeJobs.Columns[3].Visibility = Visibility.Hidden;
            dgEmployeeJobs.Columns[4].Visibility = Visibility.Hidden;

            dgEmployeeJobs.Columns[2].Header = "Linked\nJob Code";
            dgEmployeeJobs.Columns[5].Header = "Description";
            dgEmployeeJobs.Columns[5].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            if (jobCode != "")
            {
                for (int x = 0; x <= dsLinkedJobCodes.Tables[0].Rows.Count - 1; x++)
                {
                    if (dsLinkedJobCodes.Tables[0].Rows[x]["JobCode"].ToString() == jobCode)
                    {
                        row = x;
                        CurrentRowLinkedJobCodes = row;
                        break;
                    }
                }
            }

            tools.SelectDGGridRowByIndex(dgEmployeeJobs,row);

            // Disable the Context Menu if no linked jobs exist for this employee.
            if (dsLinkedJobCodes.Tables[0].Rows.Count > 0)
            { cMenuLinkedJobCodeList.Visibility = Visibility.Visible; }
            else
            { cMenuLinkedJobCodeList.Visibility = Visibility.Hidden; }

            // Disable the Remove Link option if no linked jobs exist for this employee.
            btnRemoveJobCodeLink.IsEnabled = dsLinkedJobCodes.Tables[0].Rows.Count > 0;

            //If record count is zero then disable the grid so menu options won't show.
            dgEmployeeJobs.IsEnabled = dsLinkedJobCodes.Tables[0].Rows.Count > 0;
        }

        private void LoadEmployees(int row = 0,string sqlJobCode = "")
        {
            dsEmployees.Clear();
            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmdEmployees = new OdbcCommand();
            cmdEmployees.Connection = buck.cnOdyssey;
            cmdEmployees.CommandType = CommandType.Text;

            // If this form is being called by the EMT module then only display employees that are assigned to the passed supervisor ID.
            if (employeeLookupID.Length > 0 && supervisorLookupID.Length > 0)
            {
                cmdEmployees.CommandText = @"SELECT PayrollID, Name, WorkPhone FROM Employees WHERE CompanyID = 'buck' AND Active = 1 
                AND PayrollID <> '' AND Foreman = '" + supervisorLookupID + "' ORDER BY Name";
            }
            else
            {
                cmdEmployees.CommandText = @"SELECT PayrollID, Name, WorkPhone FROM Employees WHERE CompanyID = 'buck' AND Active = 1 
                AND PayrollID <> '' ORDER BY Name";
            }

            OdbcDataAdapter daEmployees = new OdbcDataAdapter(cmdEmployees);
            daEmployees.Fill(dsEmployees);
            dsEmployees.Tables[0].DefaultView.RowFilter = null;

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;

            // Add the employee's primary job code from Odyssey to this list.
            for (int x = 0; x <= dsEmployees.Tables[0].Rows.Count - 1; x++)
            {
                CreatePersonalJobCode(x);
                //// Store search and append data.
                //string employeeID = dsEmployees.Tables[0].Rows[x]["PayrollID"].ToString();
                ////string jobCode = dsEmployees.Tables[0].Rows[x]["WorkPhone"].ToString();

                //// March 23, 2017
                //// I have decided to auto add the "Personal Job Code" as supervisors tell me
                //// that training documents are not all that related to a particular job.
                //string jobCode = "X-" + employeeID;

                //if (jobCode.ToString().Length > 0)
                //{
                //    // First, make sure the Job Code is not already assigned to the current employee
                //    cmd.CommandText = "SELECT * FROM EmployeeJobs WHERE EmployeeID = '" + employeeID + "' " +
                //        "AND JobCode = '" + jobCode + "'";

                //    DataSet ds = new DataSet();
                //    SqlDataAdapter da = new SqlDataAdapter(cmd);
                //    da.Fill(ds);

                //    int returnResults = ds.Tables[0].Rows.Count; 

                //    if (returnResults == 0)
                //    {
                //        // Linked code was not found, so add it to this employee.
                //        cmd.CommandText = "INSERT INTO EmployeeJobs (EmployeeID, JobCode, IsPrimary, SortOrder) " +
                //            "VALUES('" + employeeID + "','" + jobCode + "',1,-1)";
                //        cmd.ExecuteNonQuery();
                //    }
                //}
            }

            buck.DBCloseDatabase();

            dgEmployees.ItemsSource = dsEmployees.Tables[0].DefaultView;

            tools.ConfigureDataGridOptions(dgEmployees);

            dgEmployees.CanUserSortColumns = false;
            dgEmployees.Columns[0].Header = "Employee ID";
            dgEmployees.Columns[1].Header = "Employee Name";
            dgEmployees.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            dgEmployees.Columns[2].Visibility = Visibility.Hidden;

            buck.DBCloseOdysseyDatabase();

            if (sqlJobCode.Length > 0)
            {
                try { dsEmployees.Tables[0].Columns.Add("jobCodeFound", typeof(int)); }
                catch { }

                tools.DBOpenSQLDB();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;

                string employeeID = "";

                for (int x = 0; x <= dsEmployees.Tables[0].Rows.Count-1;x++)
                {
                    string jobCode = dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCode"].ToString();
                    employeeID = dsEmployees.Tables[0].Rows[x]["PayrollID"].ToString();

                    cmd.CommandText = "SELECT COUNT(*) FROM EmployeeJobs WHERE JobCode = " + chr39 + jobCode + chr39
                        + " AND EmployeeID = " + chr39 + employeeID + chr39;

                    int result = (int)cmd.ExecuteScalar();
                    if (result > 0)
                    {
                        dsEmployees.Tables[0].Rows[x]["jobCodeFound"] = 1;
                    }
                }
                dgEmployees.ItemsSource = null;
                var strExpr = "jobCodeFound = 1";
                dsEmployees.Tables[0].DefaultView.RowFilter = strExpr;
                dgEmployees.ItemsSource = dsEmployees.Tables[0].DefaultView;

                tools.ConfigureDataGridOptions(dgEmployees);

                dgEmployees.CanUserSortColumns = false;
                dgEmployees.Columns[0].Header = "Employee ID";
                dgEmployees.Columns[1].Header = "Employee Name";
                dgEmployees.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

                dgEmployees.Columns[2].Visibility = Visibility.Hidden;
                dgEmployees.Columns[3].Visibility = Visibility.Hidden;

            }
            tools.SelectDGGridRowByIndex(dgEmployees, row);
            LoadLinkedJobCodes(GetEmployeeID());
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

            for (int x = 0; x <= dsOdysseyJobCodes.Tables[0].Rows.Count - 1; x++)
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
            cmdSQLJobCodes.CommandText = "SELECT * FROM SOPJobCodes WHERE JobCode NOT LIKE 'DEPT%' ORDER BY JobCode";
            daSQLJobCodes.Fill(dsSQLJobCodes);

            dgJobCodes.ItemsSource = dsSQLJobCodes.Tables[0].DefaultView;
            buck.DBCloseDatabase();

            tools.ConfigureDataGridOptions(dgJobCodes);

            var gridWidth = dgJobCodes.ActualWidth - 100;
            var colWidth = dgJobCodes.Columns[1].Width.Value;
            gridWidth -= colWidth;

            dgJobCodes.CanUserSortColumns = false;
            dgJobCodes.Columns[0].Visibility = Visibility.Hidden;
            dgJobCodes.Columns[1].Header = "Job Code";
            dgJobCodes.Columns[2].Header = "Description";
            dgJobCodes.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            if (row > dsSQLJobCodes.Tables[0].Rows.Count) { row -= 1; }

            tools.SelectDGGridRowByIndex(dgJobCodes, row);
        }


        private void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowEmployees = tools.GetGridRow(dgEmployees);
            string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.lblRed.Background = Brushes.Red;
            LoadEmployees();
            LoadJobCodes();
            LoadLinkedJobCodes(GetEmployeeID(),0);
            if (employeeLookupID.Length > 0)
            {
                for (int x = 0; x <= dsEmployees.Tables[0].Rows.Count-1; x++)
                {
                    CurrentRowEmployees = x;
                    if (dsEmployees.Tables[0].Rows[x]["PayrollID"].ToString() == employeeLookupID)
                    { break;}
                }

                tools.SelectDGGridRowByIndex(dgEmployees, CurrentRowEmployees);
                LoadLinkedJobCodes(GetEmployeeID(), 0);
            }
        }

        private void AddJobCodeLink()
        {
            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;

            // Store search and append data.
            string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
            string jobCode = dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCode"].ToString();

            // First, make sure the Job Code is not already assigned to the current employee
            cmd.CommandText = "SELECT COUNT(*) FROM EmployeeJobs WHERE EmployeeID = '" + employeeID + "' " +
                "AND JobCode = '" + jobCode + "'";

            int returnResults = (int)cmd.ExecuteScalar();

            if (returnResults == 0)
            {
                // Linked code was not found, so add it to this employee.
                cmd.CommandText = "INSERT INTO EmployeeJobs (EmployeeID, JobCode) " +
                    "VALUES('" + employeeID + "','" + jobCode + "')";
                cmd.ExecuteNonQuery();

                LoadLinkedJobCodes(GetEmployeeID(), 0, jobCode);

                tools.WriteSecurityLogEntry(0, tools.logEvent_JobCodeAddedToUser, "[" + jobCode + "] - " + employeeID + " (" + tools.GetOdysseyEmployeeName(employeeID) + ")");
            }

            buck.DBCloseDatabase();
        }

        private void btnAddJobCodeLink_Click(object sender, RoutedEventArgs e)
        {
            AddJobCodeLink();
        }

        private void dgJobCodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowSOPJobCodes = tools.GetGridRow(dgJobCodes);
        }


        private string GetEmployeeID()
        {
            string employeeID = "";
            foreach (DataRowView row in dgEmployees.SelectedItems)
            {
                employeeID = row.Row.ItemArray[0].ToString();
            }
            return employeeID;
        }

        private void dgEmployees_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            LoadLinkedJobCodes(GetEmployeeID(), 0);
        }

        private void RemoveJobCodeLink()
        {
            //if (!(bool)dsLinkedJobCodes.Tables[0].Rows[CurrentRowLinkedJobCodes]["IsPrimary"])
            //{

                string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
                string jobCode = dsLinkedJobCodes.Tables[0].Rows[CurrentRowLinkedJobCodes]["JobCode"].ToString();

                int row = CurrentRowLinkedJobCodes;

                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;

                int id = (int)dsLinkedJobCodes.Tables[0].Rows[CurrentRowLinkedJobCodes]["ID"];
                cmd.CommandText = "DELETE FROM EmployeeJobs WHERE ID = " + id;
                cmd.ExecuteNonQuery();

                buck.DBCloseDatabase();

                if (CurrentRowLinkedJobCodes > dsLinkedJobCodes.Tables[0].Rows.Count - 2) CurrentRowLinkedJobCodes -= 1;
                if (CurrentRowLinkedJobCodes < 0) CurrentRowLinkedJobCodes = 0;
                LoadLinkedJobCodes(GetEmployeeID(), CurrentRowLinkedJobCodes);

                tools.WriteSecurityLogEntry(0, tools.logEvent_JobCodeRemovedFromUser, "[" + jobCode + "] - " + employeeID + " (" + tools.GetOdysseyEmployeeName(employeeID) + ")");

            //}
            //else
            //{
            //    MessageBox.Show("Cannot delete a link to the employee's Primary Job!", "Notice");
            //}
        }

        private void btnRemoveJobCodeLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveJobCodeLink();
        }

        private void dgEmployeeJobs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowLinkedJobCodes = tools.GetGridRow(dgEmployeeJobs);
        }

        private void dgEmployeeJobs_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentRowLinkedJobCodes = tools.GetGridRow(dgEmployeeJobs);
        }

        private void dgEmployeeJobs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Handles to custom coloring of the first cell in the grid.
            try
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new System.Action(() => AlterRow(dgEmployeeJobs, e)));
            }
            catch
            { }
        }

        private void AlterRow(DataGrid dg, DataGridRowEventArgs e)
        {

            try
            {
                var cell = GetCell(dg, e.Row, 2);
                if (cell == null)
                {
                    return;
                }
                else
                {
                    cell.Background = Brushes.White;
                    cell.Foreground = Brushes.Black;
                }

                DataRowView item = e.Row.Item as DataRowView;
                if (item != null)
                {
                    DataRow row = item.Row;
                    if ((bool)row["IsPrimary"] == true)
                    {
                        cell.Background = Brushes.Red;
                        cell.Foreground = Brushes.White;
                    }
                }
            }
            catch { }

        }

        public static DataGridCell GetCell(DataGrid host, DataGridRow row, int columnIndex)
        {
            if (row == null) return null;

            var presenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (presenter == null) return null;

            // Try to get the cell but it may possibly be virtualized.
            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
            if (cell == null)
            {
                //Now try to bring into view and retrieve the cell
                host.ScrollIntoView(row, host.Columns[columnIndex]);
                cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
            }
            return cell;
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                var v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T ?? GetVisualChild<T>(v);
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private void mnuItemJobCodeDescription_Click(object sender, RoutedEventArgs e)
        {
            string JobCodeDescription = dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCodeDescription"].ToString();
            InputBoxMultiLine ml = new InputBoxMultiLine("Edit Job Code Description:", JobCodeDescription);
            ml.ShowDialog();

            if (ml.itemText.Length > 0)
            {
                int curRow = CurrentRowSOPJobCodes;
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
                CurrentRowSOPJobCodes = curRow;

                // Reload the linked jobs grid in case the edited job code is being displayed there as well.
                LoadLinkedJobCodes(GetEmployeeID(), CurrentRowLinkedJobCodes);
            }
        }

        private void CreatePersonalJobCode(int _row = 0)
        {
            if (_row == 0) { _row = CurrentRowEmployees; }

            string employeeID = dsEmployees.Tables[0].Rows[_row]["PayrollID"].ToString();

                string jobCode = "X-" + dsEmployees.Tables[0].Rows[_row]["PayrollID"].ToString();
                string jobDescription = dsEmployees.Tables[0].Rows[_row]["Name"].ToString();

                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;

                // First, make sure that the new code does not already exist.
                cmd.CommandText = "SELECT COUNT(*) FROM SOPJobCodes WHERE JobCode = " + chr39 + jobCode + chr39;
                int results = (int)cmd.ExecuteScalar();

                if (results == 0)
                {
                    // Add the new job code.
                    cmd.CommandText = "INSERT INTO SOPJobCodes (JobCode, JobCodeDescription) VALUES(" + chr39 + jobCode + chr39 + "," + chr39 + jobDescription + chr39 + ")";
                    cmd.ExecuteNonQuery();

                    // Link the job code to the employee
                    cmd.CommandText = "INSERT INTO EmployeeJobs (EmployeeID, JobCode) " +
                    "VALUES('" + employeeID + "','" + jobCode + "')";
                    cmd.ExecuteNonQuery();

                    LoadLinkedJobCodes(GetEmployeeID(), 0, jobCode);
                    CurrentRowLinkedJobCodes = 0;

                    LoadJobCodes(0);

                    int row = 0;

                    for (int x = 0; x <= dsSQLJobCodes.Tables[0].Rows.Count - 1; x++)
                    {
                        row = x;

                        if (dsSQLJobCodes.Tables[0].Rows[x]["JobCode"].ToString() == jobCode)
                        {
                            break;
                        }
                    }

                    tools.SelectDGGridRowByIndex(dgJobCodes, row);
                    CurrentRowSOPJobCodes = 0;
                }

                buck.DBCloseDatabase();
            }

        private void mnuItemCreatePersonalJobCode_Click(object sender, RoutedEventArgs e)
        {
            CreatePersonalJobCode();
        }

        private void mnuItemDeleteJobCode_Click(object sender, RoutedEventArgs e)
        {
            string jobCode = dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCode"].ToString();

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;

            // First, make sure that the job code is not linked to any employee.
            cmd.CommandText = "SELECT COUNT(*) FROM EmployeeJobs WHERE JobCode = " + chr39 + jobCode + chr39;
            int results = (int)cmd.ExecuteScalar();

            if (results==0)
            {
                if (MessageBox.Show("Delete Job Code [" + jobCode + "]?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    // Go ahead and delete the selected job code.
                    int oldRow = CurrentRowSOPJobCodes;
                    cmd.CommandText = "DELETE FROM SOPJobCodes WHERE JobCode = " + chr39 + jobCode + chr39;
                    cmd.ExecuteNonQuery();

                    LoadJobCodes(oldRow);

                    CurrentRowSOPJobCodes = tools.GetGridRow(dgJobCodes);
                }
            }
            else
            {
                MessageBox.Show("Cannot delete a Job Code that is linked to one or more employees.", "Notice");
            }

            buck.DBCloseDatabase();           
        }

        private void mnuItemAddJobCode_Click(object sender, RoutedEventArgs e)
        {
            InputBoxMultiLine ml = new InputBoxMultiLine("Enter new Job Code:\n(10 characters max.)", "", 0, 0, false, false, true);
            ml.ShowDialog();

            if (ml.itemText.Length > 0)
            {
                if (ml.itemText.Length > 10) { ml.itemText = ml.itemText.Substring(0, 10); }
                ml.itemText = ml.itemText.ToUpper();

                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO SOPJobCodes (JobCode) VALUES(" + chr39 + ml.itemText + chr39 + ")";
                cmd.ExecuteNonQuery();

                LoadJobCodes(0);

                int row = 0;

                for (int x = 0; x <= dsSQLJobCodes.Tables[0].Rows.Count-1;x++)
                {
                    row = x;

                    if (dsSQLJobCodes.Tables[0].Rows[x]["JobCode"].ToString() == ml.itemText)
                    {
                        break;
                    }
                }

                tools.SelectDGGridRowByIndex(dgJobCodes, row);
                CurrentRowSOPJobCodes = row;

                InputBoxMultiLine mlDescription = new InputBoxMultiLine("Enter Description for Code:\n[" + ml.itemText + "]", "", 0, 0, false, false, true);
                mlDescription.ShowDialog();

                if (mlDescription.itemText.Length >0)
                {
                    tools.DBOpenSQLDB();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.CommandText = "UPDATE SOPJobCodes SET JobCodeDescription = " + chr39 + mlDescription.itemText + chr39
                        + " WHERE JobCode = " + chr39 + ml.itemText + chr39;
                    cmd.ExecuteNonQuery();

                    LoadJobCodes(row);
                }


                buck.DBCloseDatabase();
            }
        }

        private void txtJobCode_KeyDown(object sender, KeyEventArgs e)
        {
        }

        private void txtJobCode_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtJobCode.Text = txtJobCode.Text.ToUpper();
            int row = 0;
            bool found = false;

            for (int x = 0; x <= dsSQLJobCodes.Tables[0].Rows.Count - 1; x++)
            {
                row = x;
                if (txtJobCode.Text.Length <= dsSQLJobCodes.Tables[0].Rows[x]["JobCode"].ToString().Length)
                {
                    if (dsSQLJobCodes.Tables[0].Rows[x]["JobCode"].ToString().Substring(0, txtJobCode.Text.Length) == txtJobCode.Text.ToUpper())
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            { row = 0; }

            CurrentRowSOPJobCodes = row;
            tools.SelectDGGridRowByIndex(dgJobCodes, CurrentRowSOPJobCodes);

            txtJobCode.SelectionStart = txtJobCode.Text.Length;
            txtJobCode.Focus();
        }

        private void txtJobCode_GotFocus(object sender, RoutedEventArgs e)
        {
            txtJobCode.Background = Brushes.Yellow;
            txtEmployee.Text = "";
        }

        private void txtJobCode_LostFocus(object sender, RoutedEventArgs e)
        {
            txtJobCode.Background = Brushes.White;
        }

        private void txtEmployee_LostFocus(object sender, RoutedEventArgs e)
        {
            txtEmployee.Background = Brushes.White;
        }

        private void txtEmployee_GotFocus(object sender, RoutedEventArgs e)
        {
            txtEmployee.Background = Brushes.Yellow;
            txtJobCode.Text = "";
        }

        private void txtEmployee_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtEmployee.Text = txtEmployee.Text.ToUpper();
            int row = 0;
            bool found = false;

            if (txtEmployee.Text.Length >0)
            {
                for (int x = 0; x <= dsEmployees.Tables[0].Rows.Count - 1; x++)
                {
                    row = x;
                    if (txtEmployee.Text.Length <= dsEmployees.Tables[0].Rows[x]["Name"].ToString().Length)
                    {
                        if (dsEmployees.Tables[0].Rows[x]["Name"].ToString().Substring(0, txtEmployee.Text.Length).ToUpper() == txtEmployee.Text.ToUpper())
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            { row = 0; }

            CurrentRowEmployees = row;
            tools.SelectDGGridRowByIndex(dgEmployees, CurrentRowEmployees);

            CurrentRowEmployees = tools.GetGridRow(dgEmployees);

            string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
            LoadLinkedJobCodes(GetEmployeeID());

            txtEmployee.SelectionStart = txtEmployee.Text.Length;
            txtEmployee.Focus();
        }

        private void mnuItemFilterJobCodes_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees(0, dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCode"].ToString());
        }

        private void mnuItemShowAllJobCodes_Click(object sender, RoutedEventArgs e)
        {
            LoadEmployees(0,"");
        }

        private void EditJobCodeDocumentLinks(bool useLinkedDocumentsLink = false)
        {
            if (useLinkedDocumentsLink)
            {
                Properties.Settings.Default.SOPJobCodeLookUp = dsLinkedJobCodes.Tables[0].Rows[CurrentRowLinkedJobCodes]["JobCode"].ToString();
            }
            else
            {
                Properties.Settings.Default.SOPJobCodeLookUp = dsSQLJobCodes.Tables[0].Rows[CurrentRowSOPJobCodes]["JobCode"].ToString();
            }
            Properties.Settings.Default.Save();
            string jobCode = "X-" + dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();

            NavigationService.Navigate(new SOPTagToJobCode(employeeLookupID,supervisorLookupID,jobCode));
        }

        private void menuItemEditSOPJobCodeLinks_Click(object sender, RoutedEventArgs e)
        {
            EditJobCodeDocumentLinks();
        }

        private void mnuItemEditSOPobCodeLinks_Click(object sender, RoutedEventArgs e)
        {
            EditJobCodeDocumentLinks(true);
        }

        private void mnuItemRemoveJobCodeLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveJobCodeLink();
        }

        private void mnuItemAddJobCodeToEmployee_Click(object sender, RoutedEventArgs e)
        {
            AddJobCodeLink();
        }

        private void btnEditLinkedJobDocuments_Click(object sender, RoutedEventArgs e)
        {
            EditJobCodeDocumentLinks(true);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("EmployeeJobAssignments");
            help.ShowDialog();
        }
    }
}
