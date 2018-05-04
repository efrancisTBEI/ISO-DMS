using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Threading;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for EditUsers.xaml
    /// </summary>
    public partial class EditUsers : Window
    {

        bool blnLoading = true;
        DispatcherTimer usersTimer = new DispatcherTimer();

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsUsers = new DataSet();
        DataSet dsDepartments = new DataSet();
        DataSet dsAssignedDepartments = new DataSet();

        int CurrentUsersRow = 0;
        int CurrentDepartmentsRow = 0;
        int CurrentAssignedDepartmentsRow = 0;

        public EditUsers()
        {
            InitializeComponent();
        }

        private void usersTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                if (blnLoading)
                {
                    //tools.SelectDGGridRowByIndex(dgUsers, 0);
                    blnLoading = false;
                }
            }
        }

        private void dgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                CurrentUsersRow = tools.GetGridRow(dgUsers);

                if ((int)dsUsers.Tables[0].Rows[CurrentUsersRow]["SecurityLevel"] == SecurityLevel.SystemAdmin)
                {
                    dgDepartments.IsEnabled = false;
                    btnAddToDepartment.IsEnabled = false;
                    btnDeleteFromDepartment.IsEnabled = false;
                }
                else
                {
                    dgDepartments.IsEnabled = true;
                    btnAddToDepartment.IsEnabled = true;
                    btnDeleteFromDepartment.IsEnabled = true;
                }

                LoadAssignedDepartments(dsUsers.Tables[0].Rows[CurrentUsersRow]["Username"].ToString());
            }
            catch { }
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Users are automatically added with Read Only privileges when they start the program for the first time.", "Notice");
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            int currentRow = CurrentUsersRow;
            string userName = dsUsers.Tables[0].Rows[CurrentUsersRow]["Username"].ToString();

            EditUserSecurityLevel userSecurity = new EditUserSecurityLevel();

            userSecurity.lblUser.Content = dsUsers.Tables[0].Rows[CurrentUsersRow]["Username"].ToString();
            userSecurity.currentUserID = (int)dsUsers.Tables[0].Rows[CurrentUsersRow]["ID"];
            userSecurity.CurrentUserSecurityLevel = dsUsers.Tables[0].Rows[CurrentUsersRow]["SecurityLevel1"].ToString();

            userSecurity.ShowInTaskbar = false;
            userSecurity.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            userSecurity.ShowDialog();

            LoadUsers(CurrentUsersRow);
            LoadAssignedDepartments(userName);
            CurrentUsersRow = currentRow;
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Cannot delete a user with this version.  Change security to [ Read Only ] in order to remove privileges.", "Notice");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a timer for this form.
            usersTimer.Tick += new EventHandler(usersTimer_Tick);
            usersTimer.Interval = TimeSpan.FromMilliseconds(250);
            usersTimer.Start();

            LoadDepartments();
            LoadUsers();

            if (dsUsers.Tables[0].Rows.Count > 0)
            {
                LoadAssignedDepartments(dsUsers.Tables[0].Rows[0]["Username"].ToString());
            }
        }

        private void LoadAssignedDepartments(string Username)
        {
            dsAssignedDepartments.Clear();

            string sql = "SELECT A.*,B.DepartmentName FROM UserDepartments A, Departments B " +
                "WHERE A.DepartmentID = B.ID AND A.Username = " + tools.chr39 + Username + tools.chr39 + "  ORDER BY B.DepartmentName";

            dsAssignedDepartments = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgAssignedDepartments,false,dsAssignedDepartments);

            dgAssignedDepartments.CanUserSortColumns = false;
            dgAssignedDepartments.SelectionMode = DataGridSelectionMode.Single;

            dgAssignedDepartments.Columns[0].Visibility = Visibility.Hidden;
            dgAssignedDepartments.Columns[1].Visibility = Visibility.Hidden;
            dgAssignedDepartments.Columns[2].Visibility = Visibility.Hidden;
            dgAssignedDepartments.Columns[3].Header = "Assigned\nDepartment Names";
            dgAssignedDepartments.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(dgAssignedDepartments, 0);
        }

        private void LoadDepartments(int row = 0,string departmentName = "")
        {
            dsDepartments.Clear();

            string sql = "";

            if (departmentName == "")
            {
                sql = "SELECT * FROM Departments WHERE DepartmentName <> '' ORDER BY DepartmentName";
            }
            else
            {
                sql = "SELECT * FROM Departments WHERE DepartmentName <> '' AND DepartmentName <> " + tools.chr39 + departmentName + tools.chr39 + " " +
                    "ORDER BY DepartmentName";
            }

            dsDepartments = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgDepartments,false,dsDepartments);

            dgDepartments.RowHeaderWidth = 0;
            dgDepartments.SelectionMode = DataGridSelectionMode.Single;
            dgDepartments.Columns[0].Visibility = Visibility.Hidden;
            dgDepartments.Columns[1].Header = "Available\nDepartment Names";
            dgDepartments.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(dgDepartments, row);
        }

        private void LoadUsers(int row = 0)
        {
            dsUsers.Clear();

            string sql = "SELECT A.ID, CONVERT(VARCHAR(10), A.DateCreated, 101) AS DateCreated, A.SecurityLevel, B.SecurityLevel, A.Username " +
                " FROM Users A, UserSecurityLevels B WHERE A.SecurityLevel = B.ID ORDER BY Username";

            dsUsers = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgUsers, false, dsUsers);

            dgUsers.SelectionMode = DataGridSelectionMode.Single;

            dgUsers.Columns[0].Visibility = Visibility.Hidden;
            dgUsers.Columns[1].Header = "Date\nCreated";
            dgUsers.Columns[2].Visibility = Visibility.Hidden;
            dgUsers.Columns[3].Header = "Security\nLevel";
            dgUsers.Columns[4].Header = "User Name";
            dgUsers.Columns[4].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            tools.SelectDGGridRowByIndex(dgUsers, row);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void dgAssignedDepartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentAssignedDepartmentsRow = tools.GetGridRow(dgAssignedDepartments);

            if (dsAssignedDepartments.Tables[0].Rows.Count > 0)
            {
                LoadDepartments(0, dsAssignedDepartments.Tables[0].Rows[CurrentAssignedDepartmentsRow]["DepartmentName"].ToString());
            }
            else
            {
                LoadDepartments(0);
            }
        }

        private void btnDeleteFromDepartment_Click(object sender, RoutedEventArgs e)
        {

            int id = (int)dsAssignedDepartments.Tables[0].Rows[CurrentAssignedDepartmentsRow]["ID"];
            string userName = dsUsers.Tables[0].Rows[CurrentUsersRow]["Username"].ToString();

            string sql = "DELETE FROM UserDepartments WHERE ID = " + id;
            tools.DBExecuteNonQuery(sql);

            LoadAssignedDepartments(userName);
        }

        private void btnAddToDepartment_Click(object sender, RoutedEventArgs e)
        {
            int departmentID = (int)dsDepartments.Tables[0].Rows[CurrentDepartmentsRow]["ID"];
            string userName = dsUsers.Tables[0].Rows[CurrentUsersRow]["Username"].ToString();

            string sql = "INSERT INTO UserDepartments (Username, DepartmentID) VALUES(" + tools.chr39 + userName + tools.chr39 + "," + departmentID + ")";
            tools.DBExecuteNonQuery(sql);

            LoadAssignedDepartments(userName);
        }

        private void dgDepartments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentDepartmentsRow = tools.GetGridRow(dgDepartments);
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("EditUsers");
            help.ShowDialog();
        }
    }
}
