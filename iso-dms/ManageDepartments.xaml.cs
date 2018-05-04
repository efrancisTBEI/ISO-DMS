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
    /// Interaction logic for ManageDepartments.xaml
    /// </summary>
    public partial class ManageDepartments : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsDepartments = new DataSet();
        int currentDepartmentRow = 0;
        string sqlDepartments = "SELECT * FROM Departments WHERE DepartmentName <> '' ORDER BY DepartmentName";

        public ManageDepartments()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtLookup_GotFocus(object sender, RoutedEventArgs e)
        {
            txtLookup.Background = Brushes.Yellow;
        }

        private void txtLookup_LostFocus(object sender, RoutedEventArgs e)
        {
            txtLookup.Background = Brushes.White;
        }

        private void txtLookup_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sql = "SELECT * FROM Departments WHERE DepartmentName LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY DepartmentName";

            if (txtLookup.Text.Length > 0)
            {
                LoadDepartments(sql);
            }
            else
            {
                LoadDepartments(sqlDepartments);
            }
        }

        private void LoadDepartments(string sqlText)
        {
            dsDepartments = tools.DBCreateDataSet(sqlText);

            tools.ConfigureDataGridOptions(dgDepartments, false, dsDepartments, 0);

            dgDepartments.HeadersVisibility = DataGridHeadersVisibility.None; 
            dgDepartments.Columns[0].Visibility = Visibility.Hidden;
            dgDepartments.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            txtLookup.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDepartments(sqlDepartments);
        }

        private void dgDepartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentDepartmentRow = tools.GetGridRow(dgDepartments);
        }

        private void EditDepartments()
        {
            if (dsDepartments.Tables[0].Rows.Count > 0)
            {
                string departmentName = dsDepartments.Tables[0].Rows[currentDepartmentRow]["DepartmentName"].ToString();
                int ID = (int)dsDepartments.Tables[0].Rows[currentDepartmentRow]["ID"];
                int oldRow = currentDepartmentRow;

                InputBoxMultiLine iBox = new InputBoxMultiLine("Edit Department Name:", departmentName, 0, 0, false, false, true);
                iBox.ShowDialog();

                if (iBox.itemText.Length > 0)
                {

                    tools.DBOpenSQLDB();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("ID", ID);
                    cmd.Parameters.AddWithValue("DepartmentName", iBox.itemText.ToString());
                    cmd.CommandText = "UPDATE Departments SET DepartmentName = @DepartmentName WHERE ID = @ID";
                    cmd.ExecuteNonQuery();

                    buck.DBCloseDatabase();

                    LoadDepartments(sqlDepartments);
                    tools.SelectDGGridRowByIndex(dgDepartments, oldRow);
                    currentDepartmentRow = oldRow;
                }
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            EditDepartments();
        }

        private void AddNewDepartment()
        {
            InputBoxMultiLine iBox = new InputBoxMultiLine("Add New Department:", "", 0, 0, false, false, true);
            iBox.ShowDialog();

            // If a value was forwarded from the Input Box...
            if (iBox.itemText.Length > 0)
            {
                int row = 0;

                // Check to make sure the entry does not already exist
                string userName = Properties.Settings.Default.CurrentUsername.ToString();
                string sql = "SELECT COUNT(*) FROM Departments WHERE DepartmentName = '" + iBox.itemText.Replace("'", "").Trim() + "' ";
                int returnValue = (int)tools.DBExecuteScalar(sql);

                if (returnValue == 0)
                {
                    // Add the new department name
                    sql = "INSERT INTO Departments (DepartmentName) VALUES ('" + iBox.itemText.Replace("'", "").Trim() + "')";
                    tools.DBExecuteNonQuery(sql);
                    LoadDepartments(sqlDepartments);

                    string txt = iBox.itemText.Trim();
                    for (int x = 0; x <= dsDepartments.Tables[0].Rows.Count - 1; x += 1)
                    {
                        if (txt == (string)dsDepartments.Tables[0].Rows[x]["DepartmentName"])
                        {
                            row = x;
                            break;
                        }
                    }

                    tools.SelectDGGridRowByIndex(dgDepartments, row);
                    currentDepartmentRow = row;
                }
                else
                {
                    // Inform the user that the department name already exists.
                    MessageBox.Show("This entry already exists!", "Notice");
                }
            }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            AddNewDepartment();
        }

        private void DeleteDepartment()
        {
            if (dsDepartments.Tables[0].Rows.Count > 0)
            {
                string departmentName = dsDepartments.Tables[0].Rows[currentDepartmentRow]["DepartmentName"].ToString();

                if (MessageBox.Show("Delete the department [ " + departmentName.ToUpper() + "] ?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    // First check to make sure the User Defined Category has no document links.
                    int ID = (int)dsDepartments.Tables[0].Rows[currentDepartmentRow]["ID"];
                    string sql = "SELECT COUNT (*) FROM DocumentMaster WHERE DepartmentID = " + ID.ToString();

                    int oldRow = currentDepartmentRow;

                    int results = tools.DBExecuteScalar(sql);

                    if (results == 0)
                    {

                        sql = "SELECT COUNT (*) FROM UserDepartments WHERE DepartmentID = " + ID.ToString();
                        results = tools.DBExecuteScalar(sql);

                        if (results == 0)
                        {

                            sql = "DELETE FROM Departments WHERE ID = " + ID.ToString();
                            tools.DBExecuteNonQuery(sql);

                            LoadDepartments(sqlDepartments);

                            // If we happen to be deleting the last row in the table then highlight the previous row when re-displaying the data.
                            if (oldRow > dsDepartments.Tables[0].Rows.Count - 1)
                            {
                                oldRow -= 1;
                            }

                            tools.SelectDGGridRowByIndex(dgDepartments, oldRow);
                            currentDepartmentRow = oldRow;
                        }
                        else
                        {
                            MessageBox.Show("This Department is linked to at least one user and cannot be deleted!", "Notice");
                        }
                    }
                    else
                    {
                        MessageBox.Show("This Department is linked to at least one document and cannot be deleted!", "Notice");
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteDepartment();
        }
    }
}
