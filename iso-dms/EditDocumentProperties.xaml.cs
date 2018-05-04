using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for EditDocumentProperties.xaml
    /// </summary>
    public partial class EditDocumentProperties : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        public Char chr39 = Convert.ToChar(39);

        public int documentID = 0;

        private int currentCustomerLinkRow = 0;
        private int currentProductLinkRow = 0;
        private int currentVendorLinkRow = 0;
        private int currentUserDefinedCategoriesRow = 0;
        private int currentLinkedCategoriesRow = 0;

        public string isoType = "";
        public string isoTier = "";
        public string isoTag = "";
        public string isoRevision = "";
        public string isoDocumentName = "";
        public string isoDocumentTitle = "";
        public string isoDepartment = "";
        public string transactionDate = "";
        public double transactionAmount = 0;
        public bool IsDocumentCreator;

        int CurrentDepartmentRow = 0;
        int currentPrivateShareRow = 0;

        DataSet dsDepartments = new DataSet();
        DataSet dsCustomerDocumentLinks = new DataSet();
        DataSet dsProductDocumentLinks = new DataSet();
        DataSet dsVendorDocumentLinks = new DataSet();
        DataSet dsUserDefinedCategories = new DataSet();
        DataSet dsUserDefinedCategoryLinks = new DataSet();
        DataSet dsPrivateShares = new DataSet();

        public EditDocumentProperties()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            List<string> isoTypeList = new List<string>();
            isoTypeList.Add("Work Instruction");
            isoTypeList.Add("Process");
            isoTypeList.Add("Standard");
            isoTypeList.Add("QC");
            isoTypeList.Add("Form");
            isoTypeList.Add("SOP");
            isoTypeList.Add("Mapping");
            isoTypeList.Add("Records");
            isoTypeList.Sort();
            //cboISOType.ItemsSource = isoTypeList;

            LoadISODocumentTypes();

            List<string> isoTierList = new List<string>();
            isoTierList.Add("Level 1");
            isoTierList.Add("Level 2");
            isoTierList.Add("Level 3");
            isoTierList.Add("Level 4");
            isoTierList.Add("Level 5");
            isoTierList.Add("Level 6");
            isoTierList.Add("Level 7");
            isoTierList.Add("Level 8");
            isoTierList.Add("Level 9");
            cboISOTier.ItemsSource = isoTierList;

            cboISOType.Text = isoType;
            cboISOTier.Text = isoTier;
            txtISOTag.Text = isoTag;
            txtISORevision.Text = isoRevision;
            txtISOTitle.Text = isoDocumentTitle;

            // Fill the Department combo box.
            string sql = "SELECT * FROM Departments ORDER BY DepartmentName";
            dsDepartments = tools.DBCreateDataSet(sql);
            cboDepartment.ItemsSource = dsDepartments.Tables[0].DefaultView;
            cboDepartment.DisplayMemberPath = dsDepartments.Tables[0].Columns[1].ToString();
            cboDepartment.Text = isoDepartment;

            // If this is not the document creator, make the IsPrivate checkbox invisible.
            if (!IsDocumentCreator)
            {
                chkMakePrivate.Visibility = Visibility.Hidden;
            }

            LoadCustomerLinkedDocuments(documentID);
            LoadVendorLinkedDocuments(documentID);

            //MessageBox.Show("HI");

            LoadPrivateShares(documentID);


            if (tools.GetUserSecurityLevel(Properties.Settings.Default.CurrentUsername.ToString()) != SecurityLevel.SystemAdmin)
            {
                chkShowAllUserDefinedCategories.Visibility = Visibility.Hidden;
                LoadUserDefinedCategories();
            }
            else
            {
                chkShowAllUserDefinedCategories.IsChecked = Properties.Settings.Default.ModifyDocumentsShowAllUserDefinedCategories;
                LoadUserDefinedCategories(Properties.Settings.Default.ModifyDocumentsShowAllUserDefinedCategories);
            }

            LoadUserDefinedCategoryLinks();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpdateDocumentProperties()
        {
            GetDepartmentID();

            tools.DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = tools.cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@ISOType", this.cboISOType.Text);
            cmd.Parameters.AddWithValue("@ISOTier", this.cboISOTier.Text);
            cmd.Parameters.AddWithValue("@ISOTag", txtISOTag.Text.ToUpper());
            cmd.Parameters.AddWithValue("@ISORevision", this.txtISORevision.Text.ToUpper());
            cmd.Parameters.AddWithValue("@Title", txtISOTitle.Text);

            string sql = "";

            if (dtpTransactionDate.Text.Length > 0)
            {
                cmd.Parameters.AddWithValue("@TransactionDate", dtpTransactionDate.Text);
                cmd.Parameters.AddWithValue("@TransactionAmount", txtTransactionAmount.Text);

                sql = "UPDATE DocumentMaster SET DepartmentID = " + CurrentDepartmentRow + ", ISOType = @ISOType, ISOTier = @ISOTier, "
                    + "ISOTag = @ISOTag, ISORevision = @ISORevision, LastRevisionDate = '" + DateTime.Now.ToString() + "', RevisedBy = '" + Properties.Settings.Default.CurrentUsername + "', "
                    + "Title = @Title, IsPrivate = " + Convert.ToInt32(chkMakePrivate.IsChecked) + ", "
                    + "IsPublic = " + Convert.ToInt32(chkMakePublic.IsChecked) + ", TransactionDate = @TransactionDate, TransactionAmount = @TransactionAmount "
                    + "WHERE ID = " + documentID;
            }
            else
            {
                sql = "UPDATE DocumentMaster SET DepartmentID = " + CurrentDepartmentRow + ", ISOType = @ISOType, ISOTier = @ISOTier, "
                    + "ISOTag = @ISOTag, ISORevision = @ISORevision, LastRevisionDate = '" + DateTime.Now.ToString() + "', RevisedBy = '" + Properties.Settings.Default.CurrentUsername + "', "
                    + "Title = @Title, IsPrivate = " + Convert.ToInt32(chkMakePrivate.IsChecked) + ", "
                    + "IsPublic = " + Convert.ToInt32(chkMakePublic.IsChecked) + " "
                    + "WHERE ID = " + documentID;
            }

            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            buck.DBCloseDatabase();

            if (cboDepartment.Text == "Application Help Documents")
            {
                sql = "UPDATE HelpTopics SET PageID = " + tools.chr39 + this.txtISOTag.Text.ToUpper() + tools.chr39 +
                    " WHERE DocumentID = " + documentID;

                tools.DBExecuteNonQuery(sql);
            }

            tools.WriteSecurityLogEntry(documentID, tools.logEvent_DocumentPropertiesUpdated, isoDocumentName);
            this.Close();

        }

        // Save document properties
        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateDocumentProperties();
        }

        private void LoadISODocumentTypes()
        {
            string sql = "SELECT * FROM ISODocumentTypes ORDER BY DocumentType";
            DataSet ds = tools.DBCreateDataSet(sql);

            cboISOType.ItemsSource = ds.Tables[0].DefaultView;
            cboISOType.DisplayMemberPath = ds.Tables[0].Columns["DocumentType"].ToString();
            cboISOType.SelectedValuePath = ds.Tables[0].Columns["DocumentType"].ToString();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void cboDepartment_DropDownClosed(object sender, EventArgs e)
        {
            switch (cboDepartment.Text)
            {
                case "Application Help Documents":
                    chkMakePrivate.IsChecked = true;
                    break;
                case "Credit Card Receipts":
                    chkMakePrivate.IsChecked = true;
                    break;
                case "Amazon Invoices":
                    chkMakePrivate.IsChecked = true;
                    break;

            }
            GetDepartmentID();
        }

        private void cboISOType_DropDownClosed(object sender, EventArgs e)
        {
            switch (cboISOType.Text)
            {
                case "Work Instruction":
                    cboISOTier.Text = "Level 3";
                    break;
            }
        }

        private void cboDepartment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GetDepartmentID()
        {
            // Search the Departments dataset for the correct ID#
            for (int x = 0; x <= dsDepartments.Tables[0].Rows.Count - 1; x += 1)
            {
                if (dsDepartments.Tables[0].Rows[x]["DepartmentName"].ToString() == cboDepartment.Text)
                {
                    CurrentDepartmentRow = (int)dsDepartments.Tables[0].Rows[x]["ID"];
                    break;
                }
            }

        }

        private void chkMakePrivate_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkMakePrivate.IsChecked)
            {
                chkMakePublic.IsChecked = false;
            }
        }

        private void chkMakePublic_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)chkMakePublic.IsChecked)
            {
                chkMakePrivate.IsChecked = false;
            }
        }

        private void LoadVendorLinkedDocuments(int docID, string vendorName = "", int oldRow = 0)
        {

            dsVendorDocumentLinks.Clear();

            string sql = "SELECT * FROM ERPVendorDocumentLinks WHERE DocumentID = "
                + docID.ToString() + " ORDER BY VendorName ";

            dsVendorDocumentLinks = tools.DBCreateDataSet(sql);

            tools.ConfigureDataGridOptions(dgVendorDocumentLinks, false, dsVendorDocumentLinks);

            dgVendorDocumentLinks.HeadersVisibility = DataGridHeadersVisibility.None;
            dgVendorDocumentLinks.Columns[0].Visibility = Visibility.Hidden;
            dgVendorDocumentLinks.Columns[1].Visibility = Visibility.Hidden;
            //dgVendorDocumentLinks.Columns[3].Visibility = Visibility.Hidden;
            dgVendorDocumentLinks.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            int row = -1;
            if (vendorName.Length > 0)
            {
                for (int x = 0; x <= dsVendorDocumentLinks.Tables[0].Rows.Count - 1; x++)
                {
                    row += 1;
                    if (vendorName == dsVendorDocumentLinks.Tables[0].Rows[x]["VendorName"].ToString()) break;
                }
            }
            else
            {
                row = oldRow;

                if (row >= dsVendorDocumentLinks.Tables[0].Rows.Count)
                {
                    row -= 1;
                }
            }

            tools.SelectDGGridRowByIndex(dgVendorDocumentLinks, row);

            if (dsVendorDocumentLinks.Tables[0].Rows.Count > 0)
            {
                //LoadVendorLinkedDocuments(docID, dsVendorDocumentLinks.Tables[0].Rows[0]["VendorID"].ToString());
            }

            bool blnEnabled = dsVendorDocumentLinks.Tables[0].Rows.Count > 0;

            //btnRemoveCustomerLink.IsEnabled = blnEnabled;

        }


        private void LoadCustomerLinkedDocuments(int docID, string customerName = "", int oldRow = 0)
        {

            dsCustomerDocumentLinks.Clear();

            string sql = "SELECT * FROM ERPCustomerDocumentLinks WHERE DocumentID = "
                + docID.ToString() + " ORDER BY CustomerName ";

            dsCustomerDocumentLinks = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgCustomerDocumentLinks, false, dsCustomerDocumentLinks);

            dgCustomerDocumentLinks.HeadersVisibility = DataGridHeadersVisibility.None;
            dgCustomerDocumentLinks.Columns[0].Visibility = Visibility.Hidden;
            dgCustomerDocumentLinks.Columns[1].Visibility = Visibility.Hidden;
            dgCustomerDocumentLinks.Columns[3].Visibility = Visibility.Hidden;
            dgCustomerDocumentLinks.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            int row = -1;
            if (customerName.Length > 0)
            {
                for (int x = 0; x <= dsCustomerDocumentLinks.Tables[0].Rows.Count - 1; x++)
                {
                    row += 1;
                    if (customerName == dsCustomerDocumentLinks.Tables[0].Rows[x]["CustomerName"].ToString()) break;
                }
            }
            else
            {
                row = oldRow;

                if (row >= dsCustomerDocumentLinks.Tables[0].Rows.Count)
                {
                    row -= 1;
                }
            }

            tools.SelectDGGridRowByIndex(dgCustomerDocumentLinks, row);

            if (dsCustomerDocumentLinks.Tables[0].Rows.Count > 0)
            {
                LoadProductLinkedDocuments(docID, dsCustomerDocumentLinks.Tables[0].Rows[0]["CustomerID"].ToString());
            }

            bool blnEnabled = dsCustomerDocumentLinks.Tables[0].Rows.Count > 0;

            //btnRemoveCustomerLink.IsEnabled = blnEnabled;

        }

        private void LoadProductLinkedDocuments(int docID, string customerID, string productDescription = "", int oldRow = 0)
        {

            dsProductDocumentLinks.Clear();

            string sql = "SELECT * FROM ERPProductDocumentLinks WHERE DocumentID = " + docID.ToString()
                + " AND CustomerID = " + chr39 + customerID + chr39 + " ORDER BY ProductID";

            dsProductDocumentLinks = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgProductDocumentLinks, false, dsProductDocumentLinks);

            dgProductDocumentLinks.HeadersVisibility = DataGridHeadersVisibility.None;
            dgProductDocumentLinks.Columns[0].Visibility = Visibility.Hidden;
            dgProductDocumentLinks.Columns[1].Visibility = Visibility.Hidden;
            dgProductDocumentLinks.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            dgProductDocumentLinks.Columns[4].Visibility = Visibility.Hidden;

            int row = -1;
            if (productDescription.Length > 0)
            {
                for (int x = 0; x <= dsProductDocumentLinks.Tables[0].Rows.Count - 1; x++)
                {
                    row += 1;
                    if (productDescription == dsProductDocumentLinks.Tables[0].Rows[x]["ProductDescription"].ToString()) break;
                }
            }
            else
            {
                row = oldRow;

                if (row >= dsProductDocumentLinks.Tables[0].Rows.Count)
                {
                    row -= 1;
                }
            }

            if (dsProductDocumentLinks.Tables[0].Rows.Count > 0) { tools.SelectDGGridRowByIndex(dgProductDocumentLinks, row); }

            bool blnEnabled = dsProductDocumentLinks.Tables[0].Rows.Count > 0;

            //btnAddProductLink.IsEnabled = btnRemoveCustomerLink.IsEnabled;
            //btnRemoveProductLink.IsEnabled = blnEnabled;
        }

        private void RemoveCustomerLink()
        {
            if (dsCustomerDocumentLinks.Tables[0].Rows.Count > 0)
            {
                string customerID = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerID"].ToString();
                string customerName = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerName"].ToString();

                if (MessageBox.Show("Remove this document's link to: \n" + "[" + customerName + "]?\n\n All product links will be removed as well!", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    int row = currentCustomerLinkRow;

                    string sql = "DELETE FROM ERPCustomerDocumentLinks WHERE DocumentID = " + documentID.ToString()
                        + " AND CustomerID = " + chr39 + customerID + chr39;
                    tools.DBExecuteNonQuery(sql);

                    sql = "DELETE FROM ERPProductDocumentLinks WHERE DocumentID = " + documentID.ToString()
                        + " AND CustomerID = " + chr39 + customerID + chr39;
                    tools.DBExecuteNonQuery(sql);

                    LoadCustomerLinkedDocuments(documentID, "", row);
                }
            }
        }

        private void btnRemoveCustomerLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveCustomerLink();
        }

        private void AddVendorLink()
        {
            //InputBoxMultiLine inputBox = new InputBoxMultiLine("Enter Vendor ID to Link:", "", 0, 0, false, false, true);
            //inputBox.ShowDialog();

            //string vendorID = inputBox.itemText.Replace("'","");

            Lookup lookup = new Lookup("Supplier");
            lookup.ShowDialog();

            string vendorID = lookup.lookupID;

            if (vendorID.Length > 0)
            {
                // First, verify that the Vendor ID is valid and get the Vendor name from Odyssey.
                string vendorName = tools.GetOdysseyVendorName(vendorID).ToString();
                if (vendorName.Length > 0)
                {
                    tools.DBOpenSQLDB();

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@VendorID", vendorID);
                    //cmd.Parameters.AddWithValue("@VendorName", vendorName);
                    cmd.Parameters.AddWithValue("@DocumentID", documentID);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COUNT(*) FROM ERPVendorDocumentLinks "
                        + "WHERE VendorID = @VendorID AND DocumentID = @DocumentID";

                    int results = (int)cmd.ExecuteScalar();
                    buck.DBCloseDatabase();

                    if (results == 0)
                    {
                        // Add the Vendor ID and Linked Document ID here.
                        tools.DBOpenSQLDB();

                        cmd.Connection = tools.cnSQLDB;
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@VendorID", vendorID);
                        cmd.Parameters.AddWithValue("@VendorName", vendorName);
                        cmd.Parameters.AddWithValue("@DocumentID", documentID);
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "INSERT INTO ERPVendorDocumentLinks (VendorID, VendorName, DocumentID) "
                            + "VALUES (@VendorID, @VendorName, @DocumentID)";
                        cmd.ExecuteNonQuery();

                        buck.DBCloseDatabase();

                        int row = dsVendorDocumentLinks.Tables[0].Rows.Count;
                        LoadVendorLinkedDocuments(documentID, vendorName);
                    }
                    else
                    {
                        MessageBox.Show("This document is already linked to:\n" + "[" + vendorName + "].", "Notice");
                    }
                }
                else
                {
                    MessageBox.Show("Invalid Vendor ID. Please Re-enter.", "Notice");
                }
            }
        }

        private void RemoveVendorLink()
        {
            if (dsVendorDocumentLinks.Tables[0].Rows.Count > 0)
            {
                string vendorID = dsVendorDocumentLinks.Tables[0].Rows[currentVendorLinkRow]["VendorID"].ToString();
                string vendorName = dsVendorDocumentLinks.Tables[0].Rows[currentVendorLinkRow]["VendorName"].ToString();

                if (MessageBox.Show("Remove this document's link to: \n" + "[" + vendorName + "]?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    int row = currentVendorLinkRow;

                    string sql = "DELETE FROM ERPVendorDocumentLinks WHERE DocumentID = " + documentID.ToString()
                        + " AND VendorID = " + chr39 + vendorID + chr39;
                    tools.DBExecuteNonQuery(sql);
                    LoadVendorLinkedDocuments(documentID, "", row);
                }
            }
        }

        private void AddCustomerLink()
        {
            Lookup lookup = new Lookup("Customer");
            lookup.ShowDialog();

            string customerID = lookup.lookupID;

            if (customerID.Length > 0)
            {
                // First, verify that the Customer ID is valid and get the Customer name from Odyssey.
                string customerName = tools.GetOdysseyCustomerName(customerID).ToString();
                if (customerName.Length > 0)
                {
                    SqlCommand cmd = new SqlCommand();
                    tools.DBOpenSQLDB();

                    cmd.Connection = tools.cnSQLDB;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@CustomerID", customerID);
                    //cmd.Parameters.AddWithValue("@VendorName", vendorName);
                    cmd.Parameters.AddWithValue("@DocumentID", documentID);
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COUNT(*) FROM ERPCustomerDocumentLinks "
                        + "WHERE CustomerID = @CustomerID AND DocumentID = @DocumentID";

                    int results = (int)cmd.ExecuteScalar();
                    buck.DBCloseDatabase();

                    if (results == 0)
                    {
                        // Add the Customer ID and Linked Document ID here.
                        tools.DBOpenSQLDB();
                        cmd.Connection = tools.cnSQLDB;

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@CustomerID", customerID);
                        cmd.Parameters.AddWithValue("@CustomerName", customerName);
                        cmd.Parameters.AddWithValue("@DocumentID", documentID);
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "INSERT INTO ERPCustomerDocumentLinks (CustomerID, CustomerName, DocumentID) "
                            + "VALUES (@CustomerID, @CustomerName, @DocumentID)";
                        cmd.ExecuteNonQuery();

                        buck.DBCloseDatabase();

                        int row = dsCustomerDocumentLinks.Tables[0].Rows.Count;
                        LoadCustomerLinkedDocuments(documentID, customerName);
                    }
                    else
                    {
                        MessageBox.Show("This document is already linked to:\n" + "[" + customerName + "].", "Notice");
                    }
                }
                else
                {
                    MessageBox.Show("Invalid Customer ID. Please Re-enter.", "Notice");
                }
            }
        }

        private void btnAddCustomerLink_Click(object sender, RoutedEventArgs e)
        {
            AddCustomerLink();
        }

        private void AddProductLink()
        {
            if (dsCustomerDocumentLinks.Tables[0].Rows.Count > 0)
            {
                string customerID = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerID"].ToString();

                Lookup lookup = new Lookup("Product",customerID);
                lookup.ShowDialog();

                string productID = lookup.lookupID;

                if (productID.Length > 0)
                {
                    // First, verify that the Product ID is valid and get the Product description from Odyssey.
                    string productDescription = tools.GetOdysseyProductDescription(productID).ToString();
                    if (productDescription.Length > 0)
                    {
                        // Check to make sure the Customer ID is not already added.
                        string sql = "SELECT COUNT(*) FROM ERPProductDocumentLinks "
                            + "WHERE CustomerID = " + chr39 + customerID + chr39 + " AND ProductID = " + chr39 + productID + chr39
                            + " AND DocumentID = " + documentID.ToString();

                        int results = (int)tools.DBExecuteScalar(sql);

                        if (results == 0)
                        {
                            // Add the Customer ID and Linked Document ID here.
                            sql = "INSERT INTO ERPProductDocumentLinks (CustomerID, ProductDescription, ProductID, DocumentID) "
                                + "VALUES (" + chr39 + customerID + chr39 + "," + chr39 + productDescription + chr39 + ","
                                + chr39 + productID + chr39 + "," + documentID.ToString() + ")";
                            tools.DBExecuteNonQuery(sql);

                            int row = dsProductDocumentLinks.Tables[0].Rows.Count;
                            LoadProductLinkedDocuments(documentID, customerID, productDescription);
                        }
                        else
                        {
                            MessageBox.Show("This document is already linked to:\n" + "[" + productID + " - " + productDescription + "].", "Notice");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Invalid Product ID. Please Re-enter.", "Notice");
                    }
                }
            }
        }

        private void btnAddProductLink_Click(object sender, RoutedEventArgs e)
        {
            AddProductLink();
        }

        private void dgCustomerDocumentLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dsCustomerDocumentLinks.Tables[0].Rows.Count > 0)
            {
               // btnAddProductLink.IsEnabled = true;
                currentCustomerLinkRow = tools.GetGridRow(dgCustomerDocumentLinks);
                string customerID = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerID"].ToString();

                LoadProductLinkedDocuments(documentID, customerID);
                //btnAddProductLink.IsEnabled = false;
            }
        }

        private void RemoveProductLink()
        {
            if (dsProductDocumentLinks.Tables[0].Rows.Count > 0)
            {
                string productID = dsProductDocumentLinks.Tables[0].Rows[currentProductLinkRow]["productID"].ToString();
                string productDesciption = dsProductDocumentLinks.Tables[0].Rows[currentProductLinkRow]["ProductDescription"].ToString();

                string customerID = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerID"].ToString();
                string customerName = dsCustomerDocumentLinks.Tables[0].Rows[currentCustomerLinkRow]["CustomerName"].ToString();

                if (MessageBox.Show("Remove this document's link to: \n\n" + productID + "[" + productDesciption + "]?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    int row = currentProductLinkRow;

                    string sql = "DELETE FROM ERPProductDocumentLinks WHERE DocumentID = " + documentID.ToString()
                        + " AND CustomerID = " + chr39 + customerID + chr39 + " AND ProductID = " + chr39 + productID + chr39;
                    tools.DBExecuteNonQuery(sql);
                    LoadProductLinkedDocuments(documentID, customerID, "", row);
                }
            }
        }

        private void btnRemoveProductLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveProductLink();
        }

        private void dgProductDocumentLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentProductLinkRow = tools.GetGridRow(dgProductDocumentLinks);
            //btnRemoveCustomerLink.IsEnabled = dsProductDocumentLinks.Tables[0].Rows.Count > 0;
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("EditDocumentProperties");
            help.ShowDialog();
        }

        private void LoadPrivateShares(int documentID, int row = 0)
        {
            //dsPrivateShares.Clear();
            string sql = "SELECT * FROM SharedDocuments WHERE DocumentID = '" + documentID.ToString() + "' AND SharedBy = '" + Properties.Settings.Default.CurrentUsername + "'";
            dsPrivateShares = tools.DBCreateDataSet(sql);
            tools.ConfigureDataGridOptions(dgPrivateShares, false, dsPrivateShares, row);
            dgPrivateShares.HeadersVisibility = DataGridHeadersVisibility.None;
            dgPrivateShares.Columns[0].Visibility = Visibility.Hidden;
            dgPrivateShares.Columns[1].Visibility = Visibility.Hidden;
            dgPrivateShares.Columns[2].Visibility = Visibility.Hidden;
            dgPrivateShares.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        private void LoadUserDefinedCategories(bool showAll = false)
        {
            dsUserDefinedCategories.Clear();

            string sql = "";
            string userName = Properties.Settings.Default.CurrentUsername.ToString();
            if (!showAll)
            {
                sql = "SELECT * FROM UserDefinedCategories WHERE CategoryName <> '' AND UserName = '" + userName + "' ORDER BY CategoryName";
            }
            else
            {
                sql = "SELECT * FROM UserDefinedCategories WHERE CategoryName <> '' ORDER BY CategoryName, UserName";
            }

            dsUserDefinedCategories = tools.DBCreateDataSet(sql);

            tools.ConfigureDataGridOptions(dgUserDefinedCategories, false, dsUserDefinedCategories, 0);

            dgUserDefinedCategories.HeadersVisibility = DataGridHeadersVisibility.None;
            dgUserDefinedCategories.Columns[0].Visibility = Visibility.Hidden;
            dgUserDefinedCategories.Columns[1].Header = "Category Name";

            if (!showAll)
            {
                dgUserDefinedCategories.Columns[2].Visibility = Visibility.Hidden;
                dgUserDefinedCategories.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

        }

        private void btnAddUserDefinedCategory_Click(object sender, RoutedEventArgs e)
        {
            AddUserDefinedCategory();
        }

        private void AddUserDefinedCategory()
        {
            InputBoxMultiLine iBox = new InputBoxMultiLine("Add User Defined Category", "", 0, 0, false, false, true);
            iBox.ShowDialog();

            // If a value was forwarded from the Input Box...
            if (iBox.itemText.Length > 0)
            {

                int row = 0;

                // Check to make sure the entry does not already exist
                string userName = Properties.Settings.Default.CurrentUsername.ToString();
                string sql = "SELECT COUNT(*) FROM UserDefinedCategories WHERE CategoryName = '" + iBox.itemText.Replace("'","").Trim() + "' " +
                    "AND UserName = '" + userName + "'";
                int returnValue = (int)tools.DBExecuteScalar(sql);

                if (returnValue == 0)
                {
                    // Add the new category name
                    sql = "INSERT INTO UserDefinedCategories (CategoryName,UserName) VALUES ('" + iBox.itemText.Replace("'","").Trim() + "','" + userName + "')";
                    tools.DBExecuteNonQuery(sql);
                    LoadUserDefinedCategories();

                    string txt = iBox.itemText.Trim();
                    for (int x = 0; x <= dsUserDefinedCategories.Tables[0].Rows.Count - 1; x += 1)
                    {
                        if (txt == (string)dsUserDefinedCategories.Tables[0].Rows[x]["CategoryName"])
                        {
                            row = x;
                            break;
                        }
                    }

                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, row);
                    currentUserDefinedCategoriesRow = row;
                }
                else
                {
                    // Inform the user that the category name already exists.
                    MessageBox.Show("This entry already exists!", "Notice");
                }
            }
        }

        private void btnEditUserDefinedCategory_Click(object sender, RoutedEventArgs e)
        {
            EditUserDefinedCategories();
        }

        private void EditUserDefinedCategories()
        {
            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0)
            {
                string CategoryName = dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["CategoryName"].ToString();
                int ID = (int)dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["ID"];
                int oldRow = currentUserDefinedCategoriesRow;

                InputBoxMultiLine iBox = new InputBoxMultiLine("Edit User Defined Category", CategoryName,0,0,false,false,true);
                //iBox.itemText = CategoryName;
                iBox.ShowDialog();

                if (iBox.itemText.Length > 0)
                {
                    string sql = "UPDATE UserDefinedCategories SET CategoryName = " + tools.chr39 + iBox.itemText.Replace("'","").Trim() + tools.chr39 + " WHERE ID = " + ID;
                    tools.DBExecuteNonQuery(sql);

                    LoadUserDefinedCategories();
                    tools.SelectDGGridRowByIndex(dgUserDefinedCategories, oldRow);
                    currentUserDefinedCategoriesRow = oldRow;
                }
            }
        }

        private void dgUserDefinedCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentUserDefinedCategoriesRow = tools.GetGridRow(dgUserDefinedCategories);
        }

        private void btnDeleteUserDefinedCategory_Click(object sender, RoutedEventArgs e)
        {
            DeleteUserDefinedCategory();
        }

        private void DeleteUserDefinedCategory()
        {
            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0)
            {
                string CategoryName = dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["CategoryName"].ToString();

                if (MessageBox.Show("Delete the category [ " + CategoryName.ToUpper() + "] ?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    // First check to make sure the User Defined Category has no document links.
                    int ID = (int)dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["ID"];
                    string sql = "SELECT COUNT (*) FROM UserDefinedCategoryLinks WHERE UserDefinedCategoryID = " + ID.ToString();

                    int oldRow = currentUserDefinedCategoriesRow;

                    int results = tools.DBExecuteScalar(sql);

                    if (results == 0)
                    {
                        sql = "DELETE FROM UserDefinedCategories WHERE ID = " + ID.ToString();
                        tools.DBExecuteNonQuery(sql);

                        LoadUserDefinedCategories();

                        // If we happen to be deleting the last row in the table then highlight the previous row when re-displaying the data.
                        if (oldRow > dsUserDefinedCategories.Tables[0].Rows.Count - 1)
                        {
                            oldRow -= 1;
                        }

                        tools.SelectDGGridRowByIndex(dgUserDefinedCategories, oldRow);
                        currentUserDefinedCategoriesRow = oldRow;
                    }
                    else
                    {
                        MessageBox.Show("This User Defined Category is linked to at least one document and cannot be deleted!", "Notice");
                    }

                }
            }
        }

        private void btnAddCategoryLink_Click(object sender, RoutedEventArgs e)
        {
            AddUserDefinedCategoryLink();
        }

        private void AddUserDefinedCategoryLink()
        {
            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0)
            {
                string categoryName = dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["CategoryName"].ToString();
                int userDefinedCategoryID = (int)dsUserDefinedCategories.Tables[0].Rows[currentUserDefinedCategoriesRow]["ID"];
                string sql = "SELECT COUNT (*) FROM UserDefinedCategoryLinks WHERE UserDefinedCategoryID = " + userDefinedCategoryID.ToString() +
                    " AND DocumentID = " + documentID.ToString();

                int results = (int)tools.DBExecuteScalar(sql);
                if (results == 0)
                {
                    // Document did not exist in the links table, so add it.
                    sql = "INSERT INTO UserDefinedCategoryLinks (DocumentID, UserDefinedCategoryID) " +
                        "VALUES(" + documentID.ToString() + "," + userDefinedCategoryID.ToString() + ")";
                    tools.DBExecuteNonQuery(sql);
                    LoadUserDefinedCategoryLinks(categoryName,currentUserDefinedCategoriesRow);
                }
                else
                {
                    MessageBox.Show("[" + categoryName + "] is already linked to this document!", "Notice");
                }

            }
        }

        private void RemoveCategoryLink()
        {
            if (dsUserDefinedCategoryLinks.Tables[0].Rows.Count > 0)
            {
                int oldRow = currentLinkedCategoriesRow;
                if (oldRow == dsUserDefinedCategoryLinks.Tables[0].Rows.Count - 1) { oldRow -= 1; }

                int linkID = (int)dsUserDefinedCategoryLinks.Tables[0].Rows[currentLinkedCategoriesRow]["ID"];
                string sql = "DELETE FROM UserDefinedCategoryLinks WHERE ID = " + linkID.ToString();
                tools.DBExecuteNonQuery(sql);
                LoadUserDefinedCategoryLinks("",oldRow);
            }
        }

        private void LoadUserDefinedCategoryLinks(string categoryName = "",int oldRow = 0)
        {
            dsUserDefinedCategoryLinks.Clear();

            string sql = "SELECT A.*,B.CategoryName FROM UserDefinedCategoryLinks A, UserDefinedCategories B " +
                "WHERE A.UserDefinedCategoryID = B.ID AND A.DocumentID = " + documentID.ToString() +
                " ORDER BY B.CategoryName";
            dsUserDefinedCategoryLinks = tools.DBCreateDataSet(sql);

            tools.ConfigureDataGridOptions(dgCategoryLinks, false, dsUserDefinedCategoryLinks, oldRow);

            dgCategoryLinks.HeadersVisibility = DataGridHeadersVisibility.None;
            dgCategoryLinks.Columns[0].Visibility = Visibility.Hidden;
            dgCategoryLinks.Columns[1].Visibility = Visibility.Hidden;
            dgCategoryLinks.Columns[2].Visibility = Visibility.Hidden;
            dgCategoryLinks.Columns[3].Header = "Linked Category Name";
            dgCategoryLinks.Columns[3].Width = new DataGridLength(1, DataGridLengthUnitType.Star);

            int row = -1;
            if (categoryName.Length > 0)
            {
                for (int x = 0; x <= dsUserDefinedCategoryLinks.Tables[0].Rows.Count - 1; x++)
                {
                    row += 1;
                    if (categoryName == dsUserDefinedCategoryLinks.Tables[0].Rows[x]["CategoryName"].ToString()) break;
                }
            }
            else
            {
                row = oldRow;

                if (row >= dsUserDefinedCategoryLinks.Tables[0].Rows.Count)
                {
                    row -= 1;
                }
            }

            currentLinkedCategoriesRow = row;

            if (dsUserDefinedCategories.Tables[0].Rows.Count > 0) { tools.SelectDGGridRowByIndex(dgCategoryLinks, row); }

        }

        private void dgCategoryLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentLinkedCategoriesRow = tools.GetGridRow(dgCategoryLinks);
        }

        private void btnRemoveCategoryLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveCategoryLink();
        }

        private void btnAddVendorLink_Click(object sender, RoutedEventArgs e)
        {
            AddVendorLink();
        }

        private void btnRemoveVendorLink_Click(object sender, RoutedEventArgs e)
        {
            RemoveVendorLink();
        }

        private void dgVendorDocumentLinks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentVendorLinkRow = tools.GetGridRow(dgVendorDocumentLinks);
        }

        private void dgUserDefinedCategories_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddUserDefinedCategoryLink();
        }

        private void chkShowAllUserDefinedCategories_Click(object sender, RoutedEventArgs e)
        {
            LoadUserDefinedCategories((bool)chkShowAllUserDefinedCategories.IsChecked);
            Properties.Settings.Default.ModifyDocumentsShowAllUserDefinedCategories = (bool)chkShowAllUserDefinedCategories.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void dgPrivateShare_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentPrivateShareRow = tools.GetGridRow(dgPrivateShares);
        }

        private void AddSharedUser()
        {
            Lookup lookup = new Lookup("Users");
            lookup.ShowDialog();

            string userID = lookup.lookupID;

            if (userID.Length > 0)
            {
                string sql = "SELECT COUNT(*) FROM SharedDocuments "
                    + "WHERE SharedTo = " + chr39 + userID + chr39 + " AND DocumentID = " + documentID.ToString();

                int results = (int)tools.DBExecuteScalar(sql);
                if (results == 0)
                {
                        // Add the Vendor ID and Linked Document ID here.
                    sql = "INSERT INTO SharedDocuments (SharedBy, SharedTo, DocumentID) "
                        + "VALUES (" + chr39 + Properties.Settings.Default.CurrentUsername + chr39 + "," + chr39 + userID + chr39 + "," + documentID.ToString() + ")";
                    tools.DBExecuteNonQuery(sql);

                    int row = dsPrivateShares.Tables[0].Rows.Count;
                    LoadPrivateShares(documentID,row);
                    currentPrivateShareRow = row;
                }
                else
                {
                    MessageBox.Show("This document is already linked to:\n" + "[" + userID + "].", "Notice");
                }
            }

        }

        private void DeleteSharedUser()
        {
            if (dsPrivateShares.Tables[0].Rows.Count > 0)
            {
                int oldRow = currentPrivateShareRow;
                if (oldRow == dsPrivateShares.Tables[0].Rows.Count - 1) { oldRow -= 1; }

                int linkID = (int)dsPrivateShares.Tables[0].Rows[currentPrivateShareRow]["ID"];
                string sql = "DELETE FROM SharedDocuments WHERE ID = " + linkID.ToString();
                tools.DBExecuteNonQuery(sql);

                LoadPrivateShares(documentID, oldRow);
                currentPrivateShareRow = oldRow;
            }
        }

        private void btnAddSharedUser_Click(object sender, RoutedEventArgs e)
        {
            AddSharedUser();
        }

        private void btnDeleteSharedUser_Click(object sender, RoutedEventArgs e)
        {
            DeleteSharedUser();
        }

        private void txtTransactionAmount_KeyDown(object sender, KeyEventArgs e)
        {
            Match match = Regex.Match(txtTransactionAmount.Text, @"^\-?\(?\$?\s*\-?\s*\(?(((\d{1,3}((\,\d{3})*|\d*))?(\.\d{1,4})?)|((\d{1,3}((\,\d{3})*|\d*))(\.\d{0,4})?))\)?$");
            if (!match.Success)
            {
                MessageBox.Show("Invalid numeric entry!", "Notice", MessageBoxButton.OK, MessageBoxImage.Stop);
                txtTransactionAmount.Text = "";
                txtTransactionAmount.Focus();
            }

            //decimal x;
            //if (!decimal.TryParse(txtTransactionAmount.Text, out x))
            //{
            //    if (txtTransactionAmount.Text.Length > 0)
            //    {
            //        MessageBox.Show("Invalid numeric entry!", "Notice", MessageBoxButton.OK, MessageBoxImage.Stop);
            //        txtTransactionAmount.Text = "";
            //        txtTransactionAmount.Focus();
            //    }
            //}
            else
            {
                if (e.Key == Key.Return) { UpdateDocumentProperties(); }
            }
        }

        private void txtTransactionAmount_GotFocus(object sender, RoutedEventArgs e)
        {
            txtTransactionAmount.Background = Brushes.Yellow;
        }

        private void txtTransactionAmount_LostFocus(object sender, RoutedEventArgs e)
        {
            txtTransactionAmount.Background = Brushes.White;
        }
    }
}

