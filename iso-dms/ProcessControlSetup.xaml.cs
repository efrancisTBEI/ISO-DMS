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
    /// Interaction logic for ProcessControlSetup.xaml
    /// </summary>
    public partial class ProcessControlSetup : Page
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsSupervisors = new DataSet();
        DataSet dsEmployees = new DataSet();
        DataSet dsLinkedDocuments = new DataSet();
        DataSet dsMasterDocuments = new DataSet();

        int CurrentRowSupervisors = 0;
        int CurrentRowEmployees = 0;
        int CurrentRowLinkedDocuments = 0;
        int CurrentRowMasterDocuments = 0;

        public ProcessControlSetup()
        {
            InitializeComponent();
        }

        private void LoadSupervisors(int row = 0)
        {
            dsSupervisors.Clear();
            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmdSupervisors = new OdbcCommand();
            cmdSupervisors.Connection = buck.cnOdyssey;
            cmdSupervisors.CommandType = CommandType.Text;
            cmdSupervisors.CommandText = @"SELECT CodeID, Description FROM GeneralCodes WHERE CompanyID = 'BUCK' 
                AND CodeType = 'FRMN' AND CodeID IN(SELECT FOREMAN FROM Employees WHERE CompanyID = 'BUCK' 
                AND CodeID NOT IN('', 'FRMN') AND Description <> 'TEST' AND Active = 1) ORDER BY Description";

            OdbcDataAdapter daSupervisors = new OdbcDataAdapter(cmdSupervisors);
            daSupervisors.Fill(dsSupervisors);

            dgSupervisors.ItemsSource = dsSupervisors.Tables[0].DefaultView;

            tools.ConfigureDataGridOptions(dgSupervisors);

            dgSupervisors.Columns[0].Header = "Supervisor ID";
            dgSupervisors.Columns[1].Header = "Supervisor Name";

            buck.DBCloseOdysseyDatabase();

            tools.SelectDGGridRowByIndex(dgSupervisors, row);
        }

        private void LoadEmployees(string supervisorID, int row = 0)
        {
            dsEmployees.Clear();
            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmdEmployees = new OdbcCommand();
            cmdEmployees.Connection = buck.cnOdyssey;
            cmdEmployees.CommandType = CommandType.Text;
            cmdEmployees.CommandText = @"SELECT PayrollID, Name, WorkPhone FROM Employees WHERE CompanyID = 'buck' AND Active = 1 
                AND PayrollID <> '' AND Foreman = '" + supervisorID + "' ORDER BY Name";

            OdbcDataAdapter daEmployees = new OdbcDataAdapter(cmdEmployees);
            daEmployees.Fill(dsEmployees);

            dgEmployees.ItemsSource = dsEmployees.Tables[0].DefaultView;

            tools.ConfigureDataGridOptions(dgEmployees);

            dgEmployees.Columns[0].Header = "Employee ID";
            dgEmployees.Columns[1].Header = "Employee Name";
            dgEmployees.Columns[2].Header = "Job Code";

            buck.DBCloseOdysseyDatabase();

            if (dsEmployees.Tables[0].Rows.Count > 0)
            {
                string supervisor_ID = dsSupervisors.Tables[0].Rows[CurrentRowSupervisors]["CodeID"].ToString();
                string employee_ID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["WorkPhone"].ToString();
                LoadLinkedDocuments(supervisor_ID, employee_ID);
            }

            tools.SelectDGGridRowByIndex(dgEmployees, row);
        }

        // private void LoadLinkedDocuments(string supervisorID, string employeeID,int row = 0)
        private void LoadLinkedDocuments(string supervisorID, string employeeID = "", int row = 0)
        {
            dsLinkedDocuments.Clear();
            tools.DBOpenSQLDB();

            SqlCommand cmdLinkedDocuments = new SqlCommand();
            cmdLinkedDocuments.Connection = tools.cnSQLDB;
            cmdLinkedDocuments.CommandType = CommandType.Text;
            //cmdLinkedDocuments.CommandText = @"SELECT A.ID, A.Supervisor_ID, A.Employee_ID, B.Title FROM DocumentLink A, DocumentMaster B 
            //    WHERE A.Document_ID = B.ID AND A.Supervisor_ID = '" + supervisorID + "' AND A.Employee_ID = '" + employeeID + "'";

            cmdLinkedDocuments.CommandText = "SELECT A.*,B.ISOType, B.Title, B.ISOTag, B.IsoRevision FROM SOPJobCodeLinks A, DocumentMaster B WHERE A.Document_ID = B.ID AND A.JobCode = " + Convert.ToChar(39) + employeeID + Convert.ToChar(39) + " ORDER BY B.ISOType, B.Title";

            SqlDataAdapter daLinkedDocuments = new SqlDataAdapter(cmdLinkedDocuments);
            daLinkedDocuments.Fill(dsLinkedDocuments);

            dgLinkedDocuments.ItemsSource = dsLinkedDocuments.Tables[0].DefaultView;
            tools.ConfigureDataGridOptions(dgLinkedDocuments);

            dgLinkedDocuments.Columns[0].Visibility = Visibility.Hidden;

            //dgLinkedDocuments.Columns[1].Header = "Supervisor ID";
            //dgLinkedDocuments.Columns[2].Header = "Employee ID";
            //dgLinkedDocuments.Columns[3].Header = "Document Title";

            tools.SelectDGGridRowByIndex(dgLinkedDocuments, row);

        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            LoadSupervisors();
        }

        private void dgSupervisors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowSupervisors = tools.GetGridRow(dgSupervisors);

            string supervisorID = dsSupervisors.Tables[0].Rows[CurrentRowSupervisors]["CodeID"].ToString();
            LoadEmployees(supervisorID);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSupervisors();
            string supervisorID = dsSupervisors.Tables[0].Rows[0]["CodeID"].ToString();
            LoadEmployees(supervisorID);

            tools.LoadMasterDocs(dsMasterDocuments, dgMasterDocuments,0,"",false);
        }

        // Add highlighted ISO document to the current employee.
        private void AddMasterDocumentToEmployee()
        {
            if (dsMasterDocuments.Tables[0].Rows.Count > 0)
            {
                string supervisorID = dsSupervisors.Tables[0].Rows[CurrentRowSupervisors]["CodeID"].ToString();
                string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
                int documentID = (int)dsMasterDocuments.Tables[0].Rows[CurrentRowMasterDocuments]["ID"];
                string title = dsMasterDocuments.Tables[0].Rows[CurrentRowMasterDocuments]["Title"].ToString();

                tools.DBOpenSQLDB();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;

                // Check to see if the document has already been assigned to the highlighted employee.
                cmd.CommandText = @"SELECT COUNT(*) FROM DocumentLink WHERE Supervisor_ID = '" + supervisorID + "' AND  Employee_ID = '" + employeeID + "' AND Document_ID = " + documentID;
                int returnValue = (int)cmd.ExecuteScalar();

                if (returnValue > 0)
                {
                    // The document has already been added.
                    MessageBox.Show("This document is already assigned to the current employee!","Notice!");
                }
                else
                {
                    // The document was not previously assigned to this employee, so add it now.
                    cmd.CommandText = @"INSERT INTO DocumentLink (Supervisor_ID, Employee_ID, Document_ID) 
                            VALUES ('" + supervisorID + "','" + employeeID + "'," + documentID + ")";
                    cmd.ExecuteNonQuery();

                    tools.WriteSecurityLogEntry(documentID, "Assigned SOP to Employee #" + employeeID, title);

                }
                buck.DBCloseDatabase();
                LoadLinkedDocuments(supervisorID, employeeID);
            }
        }

        // Delete current linked document from employee
        private void DeleteLinkedDocumentFromEmployee()
        {
            string supervisorID = dsSupervisors.Tables[0].Rows[CurrentRowSupervisors]["CodeID"].ToString();
            string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["PayrollID"].ToString();
            int documentID = (int)dsLinkedDocuments.Tables[0].Rows[CurrentRowLinkedDocuments]["ID"];
            string title = dsMasterDocuments.Tables[0].Rows[CurrentRowMasterDocuments]["Title"].ToString();

            tools.DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"DELETE FROM DocumentLink WHERE ID = " + documentID;
            cmd.ExecuteNonQuery();

            tools.WriteSecurityLogEntry(documentID, "Removed SOP from Employee #" + employeeID, title);

            buck.DBCloseDatabase();

            int oldRow = CurrentRowLinkedDocuments;

            if (oldRow >= dsLinkedDocuments.Tables[0].Rows.Count - 1)
            {
                oldRow -= 1;
            }

            LoadLinkedDocuments(supervisorID, employeeID,oldRow);
        }

        private void dgMasterDocuments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void dgMasterDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowMasterDocuments = tools.GetGridRow(dgMasterDocuments);
        }

        private void dgEmployees_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowEmployees = tools.GetGridRow(dgEmployees);

            if (dsEmployees.Tables[0].Rows.Count > 0)
            {
                string supervisorID = dsSupervisors.Tables[0].Rows[CurrentRowSupervisors]["CodeID"].ToString();
                string employeeID = dsEmployees.Tables[0].Rows[CurrentRowEmployees]["WorkPhone"].ToString();
                LoadLinkedDocuments(supervisorID, employeeID);
            }
        }

        private void btnAddLinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            AddMasterDocumentToEmployee();
        }

        private void btnDeleteinkedDocument_Click(object sender, RoutedEventArgs e)
        {
            DeleteLinkedDocumentFromEmployee();
        }

        private void dgLinkedDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentRowLinkedDocuments = tools.GetGridRow(dgLinkedDocuments);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            tools.ViewPrintDocument(dsMasterDocuments, CurrentRowMasterDocuments);
        }
    }
}
