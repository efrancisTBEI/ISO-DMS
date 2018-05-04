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
    /// Interaction logic for Lookup.xaml
    /// </summary>
    public partial class Lookup : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        string lookupTable = "";
        string lookupSQL = "";
        string lookupCustomer = "";
        int lookupRow = 0;
        public string lookupID = "";

        DataSet dsLookup = new DataSet();

        public Lookup()
        {
            InitializeComponent();
        }

        public Lookup(string tableName = "Customer",string custID = "")
        {
            InitializeComponent();

            lookupTable = tableName;
            lookupCustomer = custID;

            switch (tableName)
            {

                case "Customer":
                    lookupSQL = "SELECT Customer, CoName FROM Customer WHERE CompanyID = 'BUCK' ORDER BY CoName";
                    //dsLookup = tools.DBCreateODBCDataSet(lookupSQL);
                    break;
                case "Supplier":
                    lookupSQL = "SELECT LTRIM(RTRIM(vend_num)) As VendorID, name AS Name FROM vendaddr_mst ORDER BY name";

                    IniFile ini = new IniFile(@"C:\Temp\ISO-DMS.ini");

                    SqlConnection cn = new SqlConnection();
                    cn.ConnectionString = ini.ReadValue("Database", "ConnectionString_SLDB01");
                    cn.Open();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = cn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = lookupSQL;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dsLookup);
                    cn.Close();

                    break;
                case "Product":
                    lookupSQL = "SELECT Product, Description FROM Products WHERE CompanyID = 'BUCK' AND Customer = '" + custID + "' ORDER BY CAST(Customer AS INT), Product";
                    //dsLookup = tools.DBCreateODBCDataSet(lookupSQL);
                    break;
                case "Users":
                    lookupSQL = "SELECT ID, UserName FROM Users WHERE UserName <> '" + Properties.Settings.Default.CurrentUsername + "' ORDER BY UserName";
                    dsLookup = tools.DBCreateDataSet(lookupSQL);
                    break;
            }

            
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
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

        private void windowLookup_Loaded(object sender, RoutedEventArgs e)
        {
            txtBlkBanner.Text = lookupTable + " Lookup";
            tools.ConfigureDataGridOptions(dgLookup, false, dsLookup, 0);

            if (lookupTable == "Product")
            {
                dgLookup.Columns[0].Header = "Buck ID";
                dgLookup.Columns[1].Header = "Description";
            }
            else
            {
                dgLookup.Columns[0].Header = "ID";
                dgLookup.Columns[1].Header = "Name";
            }
            dgLookup.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            txtLookup.Focus();
        }

        private void dgLookup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lookupRow = tools.GetGridRow(dgLookup);
        }

        private void SelectLookupID()
        {
            if (dsLookup.Tables[0].Rows.Count > 0)
            {
                switch (lookupTable)
                {
                    case "Customer":
                        lookupID = dsLookup.Tables[0].Rows[lookupRow]["Customer"].ToString();
                        break;
                    case "Supplier":
                        lookupID = dsLookup.Tables[0].Rows[lookupRow]["VendorID"].ToString();
                        break;
                    case "Product":
                        lookupID = dsLookup.Tables[0].Rows[lookupRow]["Product"].ToString();
                        break;
                    case "Users":
                        lookupID = dsLookup.Tables[0].Rows[lookupRow]["UserName"].ToString();
                        break;
                }
            }

            this.Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            SelectLookupID();
        }

        private void dgLookup_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectLookupID();
        }

        private void txtLookup_TextChanged(object sender, TextChangedEventArgs e)
        {
            string sql = "";
            int id = 0;

            if (Int32.TryParse(txtLookup.Text, out id))
            {
                switch (lookupTable)
                {
                    case "Customer":
                        sql = "SELECT Customer, CoName FROM Customer WHERE CompanyID = 'BUCK' AND Customer LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY Customer";
                        break;
                    case "Supplier":
                        sql = "SELECT SupplierID, PoName FROM Supplier WHERE CompanyID = 'BUCK' AND SupplierID LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY SupplierID";
                        break;
                }
            }
            else
            {
                switch (lookupTable)
                {
                    case "Customer":
                        sql = "SELECT Customer, CoName FROM Customer WHERE CompanyID = 'BUCK' AND CoName LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY CoName";
                        break;
                    case "Supplier":
                        sql = "SELECT SupplierID, PoName FROM Supplier WHERE CompanyID = 'BUCK' AND PoName LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY PoName";
                        break;
                    case "Product":
                        sql = "SELECT Product, Description FROM Products WHERE CompanyID = 'BUCK' AND Product LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' AND Customer = '" + lookupCustomer + "' ORDER BY CAST(Customer AS INT), Product";
                        break;
                    case "Users":
                        sql = "SELECT ID, UserName FROM Users WHERE UserName <> '" + Properties.Settings.Default.CurrentUsername + "' AND UserName LIKE '" + txtLookup.Text.Replace("'", "").Trim() + "%' ORDER BY UserName";
                        break;
                }
            }

            if (txtLookup.Text.Length > 0)
            {
                if (lookupTable != "Users")
                { dsLookup = tools.DBCreateODBCDataSet(sql); }
                else
                { dsLookup = tools.DBCreateDataSet(sql); }
            }
            else
            {
                if (lookupTable != "Users")
                { dsLookup = tools.DBCreateODBCDataSet(lookupSQL); }
                else
                { dsLookup = tools.DBCreateDataSet(lookupSQL); }
            }

            tools.ConfigureDataGridOptions(dgLookup, false, dsLookup, 0);

            if (lookupTable == "Product")
            {
                dgLookup.Columns[0].Header = "Buck ID";
                dgLookup.Columns[1].Header = "Description";
            }
            else
            {
                dgLookup.Columns[0].Header = "ID";
                dgLookup.Columns[1].Header = "Name";
            }

            dgLookup.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            txtLookup.Focus();
        }
    }
}
