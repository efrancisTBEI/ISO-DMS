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
    /// Interaction logic for SearchDocuments.xaml
    /// </summary>
    public partial class SearchDocuments : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();
        public Char chr39 = Convert.ToChar(39);
        bool blnLoading = true;

        // Storage for the SQL search to be built by the user.
        public string SQLText;

        public SearchDocuments()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.SQLText = "";
            this.Close();
        }

        // Tell the main program to clear any search filters and reload defaults.
        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            this.SQLText = "";
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            // Set the saved value of each checkbox.
            chkCreatedBy.IsChecked = Properties.Settings.Default.SearchCheckBoxCreatedBy;
            chkDateCreated.IsChecked = Properties.Settings.Default.SearchCheckBoxDateCreated;
            chkEffectiveDate.IsChecked = Properties.Settings.Default.SearchCheckBoxEffectiveDate;
            chkISODocTitle.IsChecked = Properties.Settings.Default.SearchCheckBoxISODocumentTitle;
            chkISODocType.IsChecked = Properties.Settings.Default.SearchCheckBoxISODocumentType;
            chkISOTierLevel.IsChecked = Properties.Settings.Default.SearchCheckBoxISOTierLevel;
            chkLastRevisionDate.IsChecked = Properties.Settings.Default.SearchCheckBoxLastRevisionDate;
            chkOfficeDocType.IsChecked = Properties.Settings.Default.SearchCheckBoxOfficeDocumentType;
            //chkRevisedBy.IsChecked = Properties.Settings.Default.SearchCheckBoxRevisedBy;
            chkDepartment.IsChecked = Properties.Settings.Default.SearchCheckBoxDepartment;
            chkUserDefinedCategories.IsChecked = Properties.Settings.Default.SearchCheckBoxUserDefinedCategories;
            chkCustomer.IsChecked = Properties.Settings.Default.SearchCheckBoxCustomer;
            chkVendor.IsChecked = Properties.Settings.Default.SearchCheckBoxVendor;
            chkProduct.IsChecked = Properties.Settings.Default.SearchCheckBoxProduct;
            chkISOTag.IsChecked = Properties.Settings.Default.SearchCheckBoxISOTag;


            // Load available choices for each combo box.
            LoadComboBoxData(this.cboISODocType, "DocumentType", "SELECT * FROM ISODocumentTypes ORDER BY DocumentType");
            LoadComboBoxData(this.cboCreatedBy, "CreatedBy", "SELECT DISTINCT(CreatedBy) FROM DocumentMaster ORDER BY CreatedBy");
            LoadComboBoxData(this.cboOfficeDocType, "DocumentType", "SELECT * FROM OfficeDocumentTypes ORDER BY DocumentType");
            LoadComboBoxData(this.cboISOTierLevel, "TierLevel", "SELECT * FROM ISOTierLevels ORDER BY TierLevel");

            LoadComboBoxData(this.cboISOTag, "ISOTag", "SELECT DISTINCT(ISOTag) FROM DocumentMaster ORDER BY ISOTag");
            LoadComboBoxData(this.cboCustomer, "CustomerName", "SELECT DISTINCT(CustomerName) FROM ERPCustomerDocumentLinks ORDER BY CustomerName");
            LoadComboBoxData(this.cboVendor, "VendorName", "SELECT DISTINCT(VendorName) FROM ERPVendorDocumentLinks ORDER BY VendorName");
            LoadComboBoxData(this.cboProduct, "ProductID", "SELECT DISTINCT(ProductID) FROM ERPProductDocumentLinks ORDER BY ProductID");

            string sql;
            if (tools.GetUserSecurityLevel(Properties.Settings.Default.CurrentUsername) == SecurityLevel.SystemAdmin)
            {
                chkShowAllUserDefinedCategories.IsChecked = Properties.Settings.Default.SearchShowAllUserDefinedCategories;
                LoadUserDefinedCategories(Properties.Settings.Default.SearchShowAllUserDefinedCategories);
                sql = "SELECT * FROM Departments ORDER BY DepartmentName";
            }
            else
            {
                chkShowAllUserDefinedCategories.Visibility = Visibility.Hidden;
                LoadUserDefinedCategories();
                sql = "SELECT * FROM Departments WHERE DepartmentName = 'Public Documents' " + 
                    "OR ID IN (SELECT DepartmentID FROM UserDepartments WHERE UserName = " + 
                    chr39 + Properties.Settings.Default.CurrentUsername + chr39 + ") ORDER BY DepartmentName";
            }
            LoadComboBoxData(this.cboDepartment,"DepartmentName",sql);


            // Set the saved value for all comboboxes.
            cboCreatedBy.Text = Properties.Settings.Default.SearchCreatedBy;
            cboISODocType.Text = Properties.Settings.Default.SearchISODocumentType;
            cboISOTierLevel.Text = Properties.Settings.Default.SearchISOTierLevel;
            cboOfficeDocType.Text = Properties.Settings.Default.SearchOfficeDocumentType;
            cboDepartment.Text = Properties.Settings.Default.SearchDepartment;
            cboUserDefinedCategories.Text = Properties.Settings.Default.SearchUserDefinedCategories;
            cboCustomer.Text = Properties.Settings.Default.SearchCustomer;
            cboVendor.Text = Properties.Settings.Default.SearchVendor;
            cboProduct.Text = Properties.Settings.Default.SearchProduct;
            cboISOTag.Text = Properties.Settings.Default.SearchISOTag;

            // Set the saved value for the Textbox
            txtISODocTitle.Text = Properties.Settings.Default.SearchISOTitleContains;

            // Set today as the default date for all date picking controls.
            dpEffectiveDateStart.Text = Properties.Settings.Default.SearchEffectiveDateStart;
            dpEffectiveDateEnd.Text = Properties.Settings.Default.SearchEffectiveDateEnd;
            dpDateCreatedStart.Text = Properties.Settings.Default.SearchDateCreatedStart;
            dpDateCreatedEnd.Text = Properties.Settings.Default.SearchDateCreatedEnd;
            dpLastRevisionDateStart.Text = Properties.Settings.Default.SearchLastRevisionDateStart;
            dpLastRevisionDateEnd.Text = Properties.Settings.Default.SearchLastRevisionDateStart;

            blnLoading = false;
        }

        private void LoadComboBoxData(ComboBox cbo, string displayField, string SQLText)
        {
            DataSet ds = tools.DBCreateDataSet(SQLText);
            cbo.ItemsSource = ds.Tables[0].DefaultView;
            cbo.DisplayMemberPath = displayField;
            if (ds.Tables[0].Rows.Count > 0)
            {
                cbo.Text = ds.Tables[0].Rows[0][displayField].ToString();
            }
        }

        private void LoadUserDefinedCategories(bool showAll = false)
        {
            string userName = Properties.Settings.Default.CurrentUsername.ToString();

            if (!showAll)
            {
                LoadComboBoxData(this.cboUserDefinedCategories, "CategoryName", "SELECT * FROM UserDefinedCategories WHERE CategoryName = '' OR UserName = '" + userName + "' ORDER BY CategoryName");
            }
            else
            {
                LoadComboBoxData(this.cboUserDefinedCategories, "CategoryName", "SELECT * FROM UserDefinedCategories ORDER BY CategoryName");
            }
        }

        // Build the SQL Command Text for this search.
        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            RunSearch();
        }

        // Build the SQL Command Text for this search.
        private void RunSearch()
        {
            bool sortByTransactionDate = false;
            bool runSearch = false;
            int CurrentUserSecurityLevel = Properties.Settings.Default.CurrentUserSecurityLevel;
            string CurrentUserName = Properties.Settings.Default.CurrentUsername;

            this.txtISODocTitle.Text = this.txtISODocTitle.Text.Replace("'", "");

            //this.SQLText = "SELECT DocumentMaster.*, Departments.DepartmentName FROM DocumentMaster LEFT OUTER JOIN Departments ON DocumentMaster.DepartmentID = Departments.ID WHERE ";
            this.SQLText = "SELECT DocumentMaster.*, Departments.DepartmentName, ERPCustomerDocumentLinks.CustomerName, ERPVendorDocumentLinks.VendorName " +
                "FROM DocumentMaster " +
                "LEFT OUTER JOIN Departments ON DocumentMaster.DepartmentID = Departments.ID " +
                "LEFT OUTER JOIN ERPCustomerDocumentLinks ON DocumentMaster.ID = ERPCustomerDocumentLinks.DocumentID " +
                "LEFT OUTER JOIN ERPVendorDocumentLinks ON DocumentMaster.ID = ERPVendorDocumentLinks.DocumentID WHERE ";

            // ISO Document Type Choices
            if (this.chkISODocType.IsChecked == true)
            {
                runSearch = true;
                this.SQLText += "ISOType = '" + cboISODocType.Text + "'";
            }

            // ISO Tier Level Choices
            if (this.chkISOTierLevel.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "ISOTier = '" + cboISOTierLevel.Text + "'";
            }

            // ISO Tag Choices
            if (this.chkISOTag.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "ISOTag = '" + cboISOTag.Text + "'";
            }

            // Document Title Substring Search.
            if (chkISODocTitle.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                SQLText += "CHARINDEX('" + this.txtISODocTitle.Text + "', Title) <> 0";
            }

            // Microsoft Office Document Types
            if (this.chkOfficeDocType.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "DocumentType = '" + this.cboOfficeDocType.Text + "'";
            }

            // Filter by CreateBy name
            if (this.chkCreatedBy.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "CreatedBy = '" + this.cboCreatedBy.Text + "'";
            }

            // Filter by Date Created
            if (this.chkDateCreated.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "DateCreated BETWEEN CONVERT(date,'" + this.dpDateCreatedStart.Text + "') AND CONVERT(date,'" + dpDateCreatedEnd.Text + "')";
            }

            // Filter by Transaction Date.
            if (this.chkEffectiveDate.IsChecked == true)
            {
                sortByTransactionDate = true;

                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "TransactionDate BETWEEN CONVERT(date,'" + this.dpEffectiveDateStart.Text + "') AND CONVERT(date,'" + this.dpEffectiveDateEnd.Text + "')";
            }

            // Filter by Date Revised.
            if (this.chkLastRevisionDate.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "CONVERT(VARCHAR(10),LastRevisionDate,101) = '" + this.dpLastRevisionDateStart.Text + "' OR CONVERT(VARCHAR(10),LastRevisionDate,101) BETWEEN '" + this.dpLastRevisionDateStart.Text + "' AND '" + this.dpLastRevisionDateEnd.Text + "'";
            }

            // Filter by Department
            if (this.chkDepartment.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                if (this.cboDepartment.Text.Trim().Length > 0)
                {
                    this.SQLText += "DepartmentName = '" + this.cboDepartment.Text.Trim() + "'";
                }
                else
                {
                    this.SQLText += "DocumentMaster.DepartmentID = 0 ";
                }
            }

            // Filter by User Defined Category
            if (this.chkUserDefinedCategories.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                sortByTransactionDate = true;
                this.SQLText += "DocumentMaster.ID IN (SELECT DocumentID FROM UserDefinedCategoryLinks " +
                    "WHERE UserDefinedCategoryID IN (SELECT ID FROM UserDefinedCategories " +
                    "WHERE CategoryName = " + tools.chr39 + this.cboUserDefinedCategories.Text + tools.chr39 + "))";
            }

            // Filter by Customer
            if (this.chkCustomer.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "DocumentMaster.ID IN (SELECT DocumentID FROM ERPCustomerDocumentLinks " +
                    "WHERE CustomerName = " + tools.chr39 + this.cboCustomer.Text + tools.chr39 + ") ";
            }

            // Filter by Vendor
            if (this.chkVendor.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "DocumentMaster.ID IN (SELECT DocumentID FROM ERPVendorDocumentLinks " +
                    "WHERE VendorName = " + tools.chr39 + this.cboVendor.Text + tools.chr39 + ") ";
            }

            // Filter by Product
            if (this.chkProduct.IsChecked == true)
            {
                if (this.SQLText.Substring(this.SQLText.Length - 6) != "WHERE ")
                {
                    this.SQLText += " AND ";
                }
                runSearch = true;
                this.SQLText += "DocumentMaster.ID IN (SELECT DocumentID FROM ERPProductDocumentLinks " +
                    "WHERE ProductID = " + tools.chr39 + this.cboProduct.Text + tools.chr39 + ") ";
            }

            if (runSearch)
            {

                switch (CurrentUserSecurityLevel)
                {
                    case SecurityLevel.ReadOnly:
                        // Documents created by this user.
                        this.SQLText += " AND (DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        // Display all public documents.
                        this.SQLText += " OR DocumentMaster.IsPublic = 1";
                        // Documents that are part of a department the user belongs to.
                        this.SQLText += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        // Do not display private documents created by other users.
                        this.SQLText += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1))";
                        break;
                    case SecurityLevel.SystemAdmin:
                        // Do not display private documents created by other users.
                        this.SQLText += " AND (NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1))";
                        // If the "Hide Revisions" checkbox is checked on the main page then hide all revised documents.
                        if (Properties.Settings.Default.HideRevisions)
                        {
                            this.SQLText += "AND IsDeprecated = 0 ";
                        }
                        if (Properties.Settings.Default.ShowISODocumentsOnly)
                        {
                            this.SQLText += "AND ISOType IN ('QC', 'SOP', 'FORM', 'MAPPING', 'PROCESS', 'RECORDS', 'STANDARD') ";
                        }
                        break;
                    case SecurityLevel.GroupAdmin:
                        //if (!sortByTransactionDate)
                        //{
                            // Documents created by this user.
                        //    this.SQLText += " AND ((DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                            // Display all public documents.
                       //     this.SQLText += " OR DocumentMaster.IsPublic = 1";
                       // }
                        // Do not display private documents created by other users.
                        //if (sortByTransactionDate)
                        //{
                            //this.SQLText += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            //    " AND DocumentMaster.IsPrivate = 1)";
                            // Documents that are part of a department the user belongs to.
                            //this.SQLText += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                                //"WHERE UserName = " + chr39 + CurrentUserName + chr39 + ") ";
                            //MessageBox.Show(SQLText);
                            // Documents that are part of a department the user belongs to.
                       //     this.SQLText += " AND DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM AdminDepartments " +
                       //             "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        //}
                       // else
                        //{
                            // Documents that are part of a department the user belongs to.
                            this.SQLText += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                                "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                            // Documents that are part of a department the user belongs to.
                            this.SQLText += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM AdminDepartments " +
                                "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        //}

                        System.IO.StreamWriter sw = new System.IO.StreamWriter(@"C:\Temp\SQL.txt");
                        sw.WriteLine(SQLText);
                        sw.Close();

                        break;
                    case SecurityLevel.GroupUser:
                        // Documents created by this user.
                        this.SQLText += " AND (DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        // Display all public documents.
                        this.SQLText += " OR DocumentMaster.IsPublic = 1";
                        // Documents that are part of a department the user belongs to.
                        this.SQLText += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        // Do not display private documents created by other users.
                        this.SQLText += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1))";
                        break;
                }

                if (sortByTransactionDate)
                { this.SQLText += " ORDER BY TransactionDate"; }
                else
                { this.SQLText += " ORDER BY DateCreated DESC"; }

            }
            else
            {
                this.SQLText = "";
            }

            // Save all search criteria
            Properties.Settings.Default.documentSearchInProgress = true;
            Properties.Settings.Default.documentSearchSQLText = this.SQLText;
            Properties.Settings.Default.SearchCreatedBy = this.cboCreatedBy.Text;
            Properties.Settings.Default.SearchDateCreatedEnd = this.dpDateCreatedEnd.Text;
            Properties.Settings.Default.SearchDateCreatedStart = this.dpDateCreatedStart.Text;
            Properties.Settings.Default.SearchEffectiveDateEnd = this.dpEffectiveDateEnd.Text;
            Properties.Settings.Default.SearchEffectiveDateStart = this.dpEffectiveDateStart.Text;
            Properties.Settings.Default.SearchISODocumentType = this.cboISODocType.Text;
            Properties.Settings.Default.SearchISOTierLevel = this.cboISOTierLevel.Text;
            Properties.Settings.Default.SearchISOTitleContains = this.txtISODocTitle.Text;
            Properties.Settings.Default.SearchLastRevisionDateEnd = this.dpLastRevisionDateEnd.Text;
            Properties.Settings.Default.SearchLastRevisionDateStart = this.dpLastRevisionDateStart.Text;
            Properties.Settings.Default.SearchOfficeDocumentType = this.cboOfficeDocType.Text;
            Properties.Settings.Default.SearchDepartment = this.cboDepartment.Text;
            Properties.Settings.Default.SearchUserDefinedCategories = this.cboUserDefinedCategories.Text;
            Properties.Settings.Default.SearchProduct = this.cboProduct.Text;
            Properties.Settings.Default.SearchCustomer = this.cboCustomer.Text;
            Properties.Settings.Default.SearchVendor = this.cboVendor.Text;
            Properties.Settings.Default.SearchISOTag = this.cboISOTag.Text;

            // Save status of each checkbox.
            Properties.Settings.Default.SearchCheckBoxCreatedBy = (bool)chkCreatedBy.IsChecked;
            Properties.Settings.Default.SearchCheckBoxDateCreated = (bool)chkDateCreated.IsChecked;
            Properties.Settings.Default.SearchCheckBoxEffectiveDate = (bool)chkEffectiveDate.IsChecked;
            Properties.Settings.Default.SearchCheckBoxISODocumentTitle = (bool)chkISODocTitle.IsChecked;
            Properties.Settings.Default.SearchCheckBoxISODocumentType = (bool)chkISODocType.IsChecked;
            Properties.Settings.Default.SearchCheckBoxISOTierLevel = (bool)chkISOTierLevel.IsChecked;
            Properties.Settings.Default.SearchCheckBoxLastRevisionDate = (bool)chkLastRevisionDate.IsChecked;
            Properties.Settings.Default.SearchCheckBoxOfficeDocumentType = (bool)chkOfficeDocType.IsChecked;
            //Properties.Settings.Default.SearchCheckBoxRevisedBy = (bool) chkRevisedBy.IsChecked;
            Properties.Settings.Default.SearchCheckBoxDepartment = (bool)chkDepartment.IsChecked;
            Properties.Settings.Default.SearchCheckBoxUserDefinedCategories = (bool)chkUserDefinedCategories.IsChecked;
            Properties.Settings.Default.SearchCheckBoxProduct = (bool)chkProduct.IsChecked;
            Properties.Settings.Default.SearchCheckBoxCustomer = (bool)chkCustomer.IsChecked;
            Properties.Settings.Default.SearchCheckBoxVendor = (bool)chkVendor.IsChecked;
            Properties.Settings.Default.SearchCheckBoxISOTag = (bool)chkISOTag.IsChecked;

            Properties.Settings.Default.Save();
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void chkISODocType_Checked(object sender, RoutedEventArgs e)
        {
            if (cboISODocType.Text == "Work Instruction")
            {
                cboISOTierLevel.Text = "Level 3";
            }
        }

        private void txtISODocTitle_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!blnLoading)
            {
                if (txtISODocTitle.Text.Length > 0) chkISODocTitle.IsChecked = true;
                else chkISODocTitle.IsChecked = false;
                    
            }
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("SearchDocuments");
            help.ShowDialog();
        }

        private void cboISODocType_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void cboISODocType_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkISODocType,cboISODocType);
        }

        private void SetCheckBoxStatus(CheckBox chkBox, ComboBox cboBox)
        {
            if (!blnLoading)
            {
                if (cboBox.Text.Length > 0) chkBox.IsChecked = true;
                else chkBox.IsChecked = false;
            }
        }

        private void SetComboBoxText(CheckBox chkBox, ComboBox cboBox)
        {
            if (!(bool)chkBox.IsChecked) cboBox.Text = "";
        }

        private void chkISODocType_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkISODocType, cboISODocType);
        }

        private void chkISOTierLevel_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkISOTierLevel, cboISOTierLevel);
        }

        private void cboISOTierLevel_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkISOTierLevel, cboISOTierLevel);
        }

        private void cboISOTag_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkISOTag, cboISOTag);
        }

        private void chkISOTag_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkISOTag, cboISOTag);
        }

        private void cboCreatedBy_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkCreatedBy, cboCreatedBy);
        }

        private void chkCreatedBy_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkCreatedBy, cboCreatedBy);
        }

        private void cboOfficeDocType_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkOfficeDocType, cboOfficeDocType);
        }

        private void chkOfficeDocType_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkOfficeDocType, cboOfficeDocType);
        }

        private void cboDepartment_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkDepartment, cboDepartment);
        }

        private void chkDepartment_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkDepartment, cboDepartment);
        }

        private void cboUserDefinedCategories_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkUserDefinedCategories, cboUserDefinedCategories);
        }

        private void chkUserDefinedCategories_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkUserDefinedCategories, cboUserDefinedCategories);
        }

        private void cboCustomer_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkCustomer, cboCustomer);
        }

        private void chkCustomer_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkCustomer, cboCustomer);
        }

        private void cboProduct_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkProduct, cboProduct);
        }

        private void chkProduct_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkProduct, cboProduct);
        }

        private void dpEffectiveDateStart_CalendarClosed(object sender, RoutedEventArgs e)
        {
        }

        private void cboVendor_DropDownClosed(object sender, EventArgs e)
        {
            SetCheckBoxStatus(chkVendor, cboVendor);
        }

        private void chkVendor_Click(object sender, RoutedEventArgs e)
        {
            SetComboBoxText(chkVendor, cboVendor);
        }

        private void chkShowAllUserDefinedCategories_Click(object sender, RoutedEventArgs e)
        {
            LoadUserDefinedCategories((bool)chkShowAllUserDefinedCategories.IsChecked);
            Properties.Settings.Default.SearchShowAllUserDefinedCategories = (bool)chkShowAllUserDefinedCategories.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void txtISODocTitle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { RunSearch(); }
        }
    }
}
