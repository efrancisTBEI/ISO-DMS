using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Threading;
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
    /// Interaction logic for ManageUDCategories.xaml
    /// </summary>
    public partial class ManageUDCategories : Window
    {

        bool blnLoading = true;
        DispatcherTimer UDCTimer = new DispatcherTimer();

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        DataSet dsMasterDocs = new DataSet();
        DataSet dsUserDefinedCategories = new DataSet();

        int CurrentUserDefinedCategoriesRow = 0;

        public ManageUDCategories()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void LoadCategories()
        {
            dsUserDefinedCategories.Clear();

            tools.DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM UserDefinedCategories ORDER BY CategoryName";

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dsUserDefinedCategories);

            dgUserDefinedCategories.ItemsSource = dsUserDefinedCategories.Tables[0].DefaultView;

            tools.ConfigureDataGridOptions(dgUserDefinedCategories);

            dgUserDefinedCategories.Columns[0].Visibility = Visibility.Hidden;
            dgUserDefinedCategories.Columns[1].Header = "Category Name";
            dgUserDefinedCategories.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            buck.DBCloseDatabase();
        }

        private void UDCTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                if (blnLoading)
                {
                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, 0);
                    blnLoading = false;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Create a timer for this form.
            UDCTimer.Tick += new EventHandler(UDCTimer_Tick);
            UDCTimer.Interval = TimeSpan.FromMilliseconds(250);
            UDCTimer.Start();

            LoadCategories();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            InputBoxMultiLine iBox = new InputBoxMultiLine("Add User Defined Category");
            iBox.ShowDialog();

            // If a value was forwarded from the Input Box...
            if (iBox.itemText.Length > 0)
            {

                int row = 0;

                // Check to make sure the entry does not already exist
                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();

                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT COUNT(*) FROM UserDefinedCategories WHERE CategoryName = " + Convert.ToChar(39) + iBox.itemText.Trim() + Convert.ToChar(39);

                int returnValue = (int) cmd.ExecuteScalar();

                if (returnValue == 0)
                {
                    // Add the new category name
                    cmd.CommandText = "INSERT INTO UserDefinedCategories (CategoryName) VALUES (" + Convert.ToChar(39) + iBox.itemText.Trim() + Convert.ToChar(39) + ")";
                    cmd.ExecuteNonQuery();
                    LoadCategories();

                    string txt = iBox.itemText.Trim();
                    for (int x = 0; x <= dsUserDefinedCategories.Tables[0].Rows.Count-1; x +=1)
                    {
                        if (txt == (string) dsUserDefinedCategories.Tables[0].Rows[x]["CategoryName"])
                        {
                            row = x;
                            break;
                        }
                    }

                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, row);
                }
                else
                {
                    // Inform the user that the category name already exists.
                    MessageBox.Show("This entry already exists!", "Notice");
                }

                
                buck.DBCloseDatabase();
            }

        }

        private void dgUserDefinedCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentUserDefinedCategoriesRow = tools.GetGridRow(dgUserDefinedCategories);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0)
            {
                string CategoryName = dsUserDefinedCategories.Tables[0].Rows[CurrentUserDefinedCategoriesRow]["CategoryName"].ToString();
                int ID = (int)dsUserDefinedCategories.Tables[0].Rows[CurrentUserDefinedCategoriesRow]["ID"];
                int oldRow = CurrentUserDefinedCategoriesRow;

                InputBoxMultiLine iBox = new InputBoxMultiLine("Edit User Defined Category", CategoryName);
                iBox.itemText = CategoryName;
                iBox.ShowDialog();

                if (iBox.itemText.Length > 0)
                {
                    tools.DBOpenSQLDB();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "UPDATE UserDefinedCategories SET CategoryName = " + Convert.ToChar(39) + iBox.itemText.Trim() + Convert.ToChar(39) + " WHERE ID = " + ID;
                    cmd.ExecuteNonQuery();

                    LoadCategories();
                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, oldRow);
                    CurrentUserDefinedCategoriesRow = oldRow;

                    buck.DBCloseDatabase();
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0)
            {
                string CategoryName = dsUserDefinedCategories.Tables[0].Rows[CurrentUserDefinedCategoriesRow]["CategoryName"].ToString();

                if (MessageBox.Show("Delete the category [ " + CategoryName.ToUpper() + "] ?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    int ID = (int)dsUserDefinedCategories.Tables[0].Rows[CurrentUserDefinedCategoriesRow]["ID"];
                    int oldRow = CurrentUserDefinedCategoriesRow;

                    tools.DBOpenSQLDB();
                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM UserDefinedCategories WHERE ID = " + ID;
                    cmd.ExecuteNonQuery();

                    LoadCategories();

                    // If we happen to be deleting the last row in the table then highlight the previous row when re-displaying the data.
                    if (oldRow > dsUserDefinedCategories.Tables[0].Rows.Count - 1)
                    {
                        oldRow -= 1;
                    }

                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, oldRow);
                    CurrentUserDefinedCategoriesRow = oldRow;

                    buck.DBCloseDatabase();
                }
            }
        }
    }
}
