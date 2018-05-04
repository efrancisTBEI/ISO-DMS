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

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for EditUserSecurityLevel.xaml
    /// </summary>
    public partial class EditUserSecurityLevel : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        public char chr39 = Convert.ToChar(39);

        DataSet dsSecurityLevels = new DataSet();
        int CurrentSecurityLevelsRow = 0;
        public int currentUserID = 0;
        public string CurrentUserSecurityLevel = "";

        public EditUserSecurityLevel()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSecurityLevels();
            cboSecurityLevels.Text = CurrentUserSecurityLevel;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void GetSecurityLevel()
        {
            // Search the Departments dataset for the correct ID#
            for (int x = 0; x <= dsSecurityLevels.Tables[0].Rows.Count - 1; x += 1)
            {
                if (dsSecurityLevels.Tables[0].Rows[x]["SecurityLevel"].ToString() == cboSecurityLevels.Text)
                {
                    CurrentSecurityLevelsRow = (int)dsSecurityLevels.Tables[0].Rows[x]["ID"];
                    break;
                }
            }

        }

        private void LoadSecurityLevels()
        {
            dsSecurityLevels.Clear();

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM UserSecurityLevels ORDER BY ID";

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dsSecurityLevels);

            cboSecurityLevels.ItemsSource = dsSecurityLevels.Tables[0].DefaultView;
            cboSecurityLevels.DisplayMemberPath = "SecurityLevel";
            cboSecurityLevels.Text = dsSecurityLevels.Tables[0].Rows[0]["SecurityLevel"].ToString();

            buck.DBCloseDatabase();
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            GetSecurityLevel();

            tools.DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"UPDATE Users SET SecurityLevel = " + CurrentSecurityLevelsRow + " "
                + "WHERE ID = " + currentUserID;

            cmd.ExecuteNonQuery();
            tools.WriteSecurityLogEntry(0, tools.logEvent_UserSecurityLevelUpdated, lblUser.Content.ToString());

            buck.DBCloseDatabase();

            this.Close();

        }
    }
}
