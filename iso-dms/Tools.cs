using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using TwainScanning;
using TwainScanning.Collectors;
using TwainScanning.NativeStructs;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Office.Interop.Word;
using Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.PowerPoint;
using Microsoft.Win32;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace ISO_DMS
{
    // Public references for the entire project.
    public static class SecurityLevel
    {
        public const int ReadOnly = 0;
        public const int SystemAdmin = 1;
        public const int GroupAdmin = 2;
        public const int GroupUser = 3;
    }

    public class Tools
    {
        public int HelpDepartmentID = 35;

        public string logEvent_LoggedIn = "Logged In";
        public string logEvent_LoggedOut = "Logged Out";
        public string logEvent_DeletedDocument = "Deleted Document";
        public string logEvent_ModifiedDocument = "Modified Document";
        public string logEvent_PrintedDocument = "Printed Document";
        public string logEvent_ViewedDocument = "Viewed Document";
        public string logEvent_EmailedDocument = "Emailed Document";
        public string logEvent_CheckedDocumentIn = "Checked Document In";
        public string logEvent_CheckedDocumentOut = "Checked Document Out";
        public string logEvent_DocumentPropertiesUpdated = "Document Properties Updated";
        public string logEvent_UserSecurityLevelUpdated = "User Security Level Updated";
        public string logEvent_JobCodeAddedToUser = "Job Code Added to User";
        public string logEvent_JobCodeRemovedFromUser = "Job Code Removed From User";

        public string ControlledDocPath = @"C:\Earl\ISO\Documents\Controlled\";
        public string UncontrolledDocPath = @"C:\Earl\ISO\Documents\Uncontrolled\";
        public string DeletedDocPath = @"C:\Earl\ISO\Documents\Deleted\";

        public BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        public BuckUtils.Msg msg = new BuckUtils.Msg();

        public SqlConnection cnSQLDB = new SqlConnection();
        public string dbConnectionString;

        //public string dbServer = @"SLDB02";
        //public string dbName = "ISO-DMS";
        //public string cnConnectionString = @"SERVER=SLDB02;DATABASE=ISO-DMS;TRUSTED_CONNECTION=YES;INTEGRATED SECURITY = SSPI; MultipleActiveResultSets=True";

        public Char chr39 = Convert.ToChar(39);

        public string MySqlEscape(Object usString)
        {
            if (usString is DBNull)
            {
                return "";
            }
            else
            {
                string sample = Convert.ToString(usString);
                return Regex.Replace(sample, @"[\r\n\x00\x1a\\'""]", @"\$0");
            }
        }

        static IImageCollector ScanMinimal(string fileName)
        {

            Tools tools = new Tools();

            var collector = new ImageMultiCollector();
            AppInfo info = new AppInfo();
            info.name = "Terminal";
            info.manufacturer = "terminalworks";
            try
            {
                using (DataSourceManager dsm = new DataSourceManager(IntPtr.Zero, info))
                {
                    //dsm.SelectDefaultSourceDlg();
                    using (var ds = dsm.OpenSource())
                    {
                        ds.UseDuplex.Value = true;
                        ds.ColorMode.Value = TwPixelType.RGB;

                        if (ds == null)
                        {
                            Console.WriteLine("Unable to open source");
                            return null;
                        }
                        ImageCollector imgCol = new ImageCollector();
                        collector.AddCollector(imgCol);
                        DataSource.ErrorInfo ei = new DataSource.ErrorInfo();
                        //var collector = ds.Acquire(false, true, ei, TwSX.Native);
                        ds.Acquire(collector, false, false);
                        imgCol.SaveAllToMultipagePdf(fileName);
                        return collector;
                    }
                }
            }
            catch (BadRcTwainException ex)
            {
                Console.Write("Bad twain return code: " + ex.ReturnCode.ToString() + "\nCondition code: " + ex.ConditionCode.ToString() + "\n" + ex.Message);
            }
            return new ImageCollector();
        }

        public string QuoteCheck(string instring)
        {
            string outString = instring.Replace("'", @"\'''");
            return outString;
        }

        public bool DBOpenSQLDB01()
        {
            IniFile ini = new IniFile(@"C:\Temp\ISO-DMS.ini");

            bool success = true;

            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = ini.ReadValue("Database", "ConnectionString01");

            try
            { cn.Open(); } catch { success = false; }

            return success;
        }

        public bool DBOpenSQLDB()
        {
            IniFile ini = new IniFile(@"C:\Temp\ISO-DMS.ini");

            string dbServer = ini.ReadValue("Database", "ServerName");
            string dbName = ini.ReadValue("Database", "DatabaseName");

            bool success = true;
            if (cnSQLDB.State != ConnectionState.Open)
            {
                dbConnectionString = "SERVER=" + dbServer + ";DATABASE=" + dbName + ";TRUSTED_CONNECTION=YES;INTEGRATED SECURITY = SSPI; MultipleActiveResultSets=True"; ;
                cnSQLDB.ConnectionString = dbConnectionString;

                try { cnSQLDB.Open(); }
                catch { success = false; }
            }

            return success;
        }

        public void DBCloseSQLDatabase()
        {
            if (cnSQLDB.State != ConnectionState.Closed) { cnSQLDB.Close(); }
        }

        public bool IsUserADepartmentAdmin(string UserName, int DepartmentID)
        {
            bool returnValue = false;

            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM AdminDepartments WHERE Username = '" + UserName + "' " +
                "AND DepartmentID = " + DepartmentID;

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            buck.DBCloseDatabase();

            if (ds.Tables[0].Rows.Count > 0)
            {
                returnValue = true;
            }

            return returnValue;
        }

        public int GetUserSecurityLevel(string UserName)
        {
            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;

            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT SecurityLevel FROM Users WHERE Username = '" + UserName + "'";

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            buck.DBCloseDatabase();

            int returnValue = 0;

            if (ds.Tables[0].Rows.Count == 0)
            {
                // User not found - this is most likely the first time they have logged in.
                return returnValue;
            }
            else
            {
                return (int)ds.Tables[0].Rows[0]["SecurityLevel"];
            }

        }

        public void CheckUserCredentials(string UserName)
        {

            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = '" + UserName + "'";

            int returnValue = (int)cmd.ExecuteScalar();

            if (returnValue == 0)
            {
                string message = "Since this is your first time logging into the ISO-DMS system your "
                  + " priviledges are limited to read only access.  An email notification has been "
                + "generated for the program administrator and if required your privileges will be "
                + "upgraded accordingly.";

                // Add the user here:
                cmd.CommandText = "INSERT INTO Users (Username) VALUES('" + UserName + "')";
                cmd.ExecuteNonQuery();

                // Email system admin here with notice of a new user logging in.
                System.Net.Mail.MailMessage msg = new System.Net.Mail.MailMessage();
                msg.From = new MailAddress("buckcompany@buckcompany.com", "Buck Company ISO Administrator");
                msg.To.Add(new MailAddress("earl.francis@buckcompany.com"));

                msg.Subject = "Buck Company ISO-DMS Notification";
                msg.Body = "A new user has logged into the ISO-DMS system.  Please check to see if a security upgrade is required.\n\n";
                msg.Body += "Username:  " + UserName;

                // Send the email
                SmtpClient smtpServer = new SmtpClient();
                smtpServer.Credentials = new NetworkCredential("buckcompany@buckcompany.com", "hzdbeyz998kr");
                smtpServer.Port = 587;
                smtpServer.Host = "smtp.gmail.com";
                smtpServer.EnableSsl = true;
                //smtpServer.Send(msg);
                buck.DoEvents();

                // Write a security log entry
                WriteSecurityLogEntry(0, "New User Logged In", buck.GetCurrentUserName());
                MessageBox.Show(message, "Notice");
            }
            else
            {

            }

            buck.DBCloseDatabase();
        }

        public void SelectDGGridRowByIndex(DataGrid dg, int RowIndex)
        {
            try
            {
                if (dg.Items.Count >= 1)
                {
                    object item = dg.Items[RowIndex];
                    dg.SelectedItem = item;
                    dg.ScrollIntoView(item);
                    DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(RowIndex) as DataGridRow;
                    row.MoveFocus(new System.Windows.Input.TraversalRequest(FocusNavigationDirection.First));
                    dg.Focus();
                }
            }
            catch
            {
            }
        }

        public void SendToPrinter(string filename)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.Verb = "print";
            info.FileName = filename;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            Process p = new Process();
            p.StartInfo = info;
            p.Start();
            p.WaitForExit();
        }

        public int GetGridRow(DataGrid dg)
        {
            int intRow = dg.Items.IndexOf(dg.CurrentItem);
            if (intRow == -1) intRow = 0;
            return intRow;
        }

        public string RemoveReservedFileSystemCharacters(string inBoundString)
        {
            string outBoundString = inBoundString;
            outBoundString = outBoundString.Replace("<", "");
            outBoundString = outBoundString.Replace(">", "");
            outBoundString = outBoundString.Replace(":", "");
            outBoundString = outBoundString.Replace(@"""", "");
            outBoundString = outBoundString.Replace("/", "");
            outBoundString = outBoundString.Replace(@"\", "");
            outBoundString = outBoundString.Replace("|", "");
            outBoundString = outBoundString.Replace("?", "");
            outBoundString = outBoundString.Replace("*", "");
            return outBoundString;
        }

        public void WriteSecurityLogEntry(int DocumentID, string Event, string Title)
        {
            string logDate = DateTime.Now.ToString();
            string userName = Properties.Settings.Default.CurrentUsername;
            string computerName = buck.GetComputerName();
            string ipAddress = buck.GetIPAddress();

            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = dbConnectionString;
            cn.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.CommandType = CommandType.Text;
            cmd.Parameters.AddWithValue("@Title", Title);
            cmd.CommandText = @"INSERT INTO SecurityLog (DocumentID, UserName, LogDate, Event, Title, ComputerName, IPAddress)
                            VALUES(" + DocumentID + ",'" + userName + "','" + logDate + "','" + Event + "',@Title,'"
                            + computerName + "','" + ipAddress + "')";

            cmd.ExecuteNonQuery();
            cn.Close();

        }

        public void StartMSWord()
        {
            string processFilename = Microsoft.Win32.Registry.LocalMachine
                .OpenSubKey("Software")
                .OpenSubKey("Microsoft")
                .OpenSubKey("Windows")
                .OpenSubKey("CurrentVersion")
                .OpenSubKey("App Paths")
                .OpenSubKey("WinWord.exe")
                .GetValue(String.Empty).ToString();

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = processFilename;
            info.CreateNoWindow = false;
            info.UseShellExecute = false;

            Process p = Process.Start(info);
        }

        public void RefreshPowerPointDocument(object controlledDocumentPath, string uncontrolledDocumentPath)
        {
            Microsoft.Office.Core.MsoTriState ofalse = Microsoft.Office.Core.MsoTriState.msoFalse;
            PowerPoint.Application ppApp = new PowerPoint.Application();

            PowerPoint.Presentations ppPres = ppApp.Presentations;
            object paramMissing = Type.Missing;


            PowerPoint.Presentation ppDoc = ppPres.Open(controlledDocumentPath.ToString(), ofalse, ofalse, ofalse);

            ppDoc.SaveAs(uncontrolledDocumentPath, PpSaveAsFileType.ppSaveAsPDF);

        }

        public void RefreshExcelDocument(object controlledDocumentPath, string uncontrolledDocumentPath)
        {
            Excel.Application excelApplication = new Excel.Application();
            object paramMissing = Type.Missing;

            // Open the source document.
            Workbook workBook = excelApplication.Workbooks.Open(controlledDocumentPath.ToString(),
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing);

            workBook.ExportAsFixedFormat(XlFixedFormatType.xlTypePDF, uncontrolledDocumentPath);

            workBook.Close();
            excelApplication.Quit();
            workBook = null;
            excelApplication = null;
        }

        public void RefreshWordDocument(object controlledDocumentPath, string uncontrolledDocumentPath)
        {
            Word.Application wordApplication = new Word.Application();
            Word.Document wordDocument = null;

            object paramMissing = Type.Missing;

            WdExportFormat paramExportFormat = WdExportFormat.wdExportFormatPDF;
            bool paramOpenAfterExport = false;
            WdExportOptimizeFor paramExportOptimizeFor =
                WdExportOptimizeFor.wdExportOptimizeForPrint;
            WdExportRange paramExportRange = WdExportRange.wdExportAllDocument;
            int paramStartPage = 0;
            int paramEndPage = 0;
            WdExportItem paramExportItem = WdExportItem.wdExportDocumentContent;
            bool paramIncludeDocProps = true;
            bool paramKeepIRM = true;
            WdExportCreateBookmarks paramCreateBookmarks =
                WdExportCreateBookmarks.wdExportCreateWordBookmarks;
            bool paramDocStructureTags = true;
            bool paramBitmapMissingFonts = true;
            bool paramUseISO19005_1 = false;

            try
            {
                // Open the source document.
                wordDocument = wordApplication.Documents.Open(
                    ref controlledDocumentPath, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing);

                // Export it in the specified format.
                if (wordDocument != null)
                {
                    wordDocument.ExportAsFixedFormat(uncontrolledDocumentPath,
                        paramExportFormat, paramOpenAfterExport,
                        paramExportOptimizeFor, paramExportRange, paramStartPage,
                        paramEndPage, paramExportItem, paramIncludeDocProps,
                        paramKeepIRM, paramCreateBookmarks, paramDocStructureTags,
                        paramBitmapMissingFonts, paramUseISO19005_1,
                        ref paramMissing);
                }
            }
            catch { }

            wordDocument.Close();
            int res = System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApplication);

            // Wait for the watermark to be added to the document.
            System.Threading.Thread.Sleep(1000);

        }

        public string AddWatermarkToPDF(string inFilename, int documentID, string docTitle = "")
        {

            string outFilename = "";
            if (docTitle == "")
            {
                outFilename = @"C:\Temp\BuckISODocument_" + documentID.ToString("000000") + ".pdf";
            }
            else
            {
                outFilename = @"C:\Temp\" + docTitle  + ".pdf";
            }
            File.Copy(inFilename, @"C:\Temp\Temp.pdf", true);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.Arguments = @" /c \\buckcofs1\fs\PublicApps\VBApps\ISO-DMS\cpdf.exe -stamp-on \\buckcofs1\fs\PublicApps\VBApps\ISO-DMS\watermark.pdf c:\temp\temp.pdf -o c:\temp\out.pdf";
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.UseShellExecute = false;

            Process p = Process.Start(info);
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            // Wait for the watermark to be added to the document.
            System.Threading.Thread.Sleep(2000);

            //if (File.Exists(outFilename)) File.Delete(outFilename);

            File.Move(@"C:\Temp\Out.pdf",outFilename);
            File.Delete(@"C:\Temp\Temp.pdf");

            return outFilename;
        }

        public void ViewPrintDocument(DataSet ds1, int rowNumber, string controlledDocPath = "", string uncontrolledDocPath = "")
        {

            MsgBox msg = new MsgBox("Preparing the requested document for viewing/printing ...please stand by.");
            msg.Show();
            buck.DoEvents();

            foreach (string f in Directory.EnumerateFiles(@"C:\Temp", "Out*.pdf"))
            {
                try
                {
                    File.Delete(f);
                }
                catch { }
            }

            string strSQL = "SELECT * FROM DocumentMaster WHERE ID = " + rowNumber;
            DataSet ds = DBCreateDataSet(strSQL);

            int currentDocumentID = (int)ds.Tables[0].Rows[0]["ID"];
            string title = ds.Tables[0].Rows[0]["Title"].ToString();

            WriteSecurityLogEntry(currentDocumentID, logEvent_ViewedDocument, title);

            string fileName = ds.Tables[0].Rows[0]["ControlledFileLink"].ToString();

            // This list contains ISO document types that will be reproduced with a watermark.
            List<string> ISODocTypes = new List<string> { "QC", "SOP" };

            if (File.Exists(fileName))
            {
                //if (ISODocTypes.Contains(ds.Tables[0].Rows[0]["ISOType"].ToString()))
                //{
                //    File.Copy(fileName, inFile, true);

                //    ProcessStartInfo info = new ProcessStartInfo();
                //    info.FileName = "cmd.exe";
                //    info.Arguments = @" /c \\buckcofs1\fs\PublicApps\VBApps\ISO-DMS\cpdf.exe -stamp-on \\buckcofs1\fs\PublicApps\VBApps\ISO-DMS\watermark.pdf " + inFile + " -o " + outFile;
                //    info.CreateNoWindow = true;
                //    info.WindowStyle = ProcessWindowStyle.Hidden;
                //    info.UseShellExecute = false;

                //    Process p = Process.Start(info);
                //    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                //    // Wait for the watermark to be added to the document.
                //    p.WaitForExit();
                //}
                //else
                //{
                //    File.Copy(fileName, outFile, true);
                //}

                //MessageBox.Show(fileName);

                System.Diagnostics.Process.Start(fileName);

                //if (File.Exists(inFile))
                //{
                //    File.Delete(inFile);
                //}

                msg.Close();

            }
            else
            {
                msg.Close();
                MessageBox.Show("The requested file cannot be found.  It may have been deleted by an administrator.", "Notice", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

        }

        private void LoadComboBoxData(ComboBox cbo, string displayField, string SQLText)
        {
            DataSet ds = DBCreateDataSet(SQLText);

            cbo.ItemsSource = ds.Tables[0].DefaultView;
            cbo.DisplayMemberPath = displayField;
            cbo.Text = ds.Tables[0].Rows[0][displayField].ToString();

            buck.DBCloseDatabase();
        }

        public void DBInsertMasterDocument(string fileToCopy, string docTitle, string controlledFilePath, string uncontrolledFilePath, string docType, DataSet ds, DataGrid dg)
        {

            List<string> docTypes = new List<string>();
            docTypes.Add("XLS");
            docTypes.Add("XLSX");
            docTypes.Add("DOC");
            docTypes.Add("DOCX");
            docTypes.Add("PPT");
            docTypes.Add("PPTX");

            string sql = "INSERT INTO DocumentMaster (Title, CreatedBy, RevisedBy, ControlledFileLink, UncontrolledFileLink, DocumentType) VALUES(" + chr39 +
                docTitle.Replace("'","") +  chr39 + "," + chr39 + buck.GetCurrentUserName() + chr39 + "," + chr39 + buck.GetCurrentUserName() + chr39 + "," + chr39 + 
                controlledFilePath + chr39 + "," + chr39 + uncontrolledFilePath + chr39 + "," + chr39 + docType + chr39 + ")";

            //try {
                File.Copy(fileToCopy, controlledFilePath, true);
                DBExecuteNonQuery(sql);
                LoadMasterDocs(ds, dg, 0, "", true, Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly);
            //}
            //catch
            //{
                //MessageBox.Show("An error occurred and this file could not be saved!", "");
            //}

        }


        public void LoadMasterDocs(DataSet ds, DataGrid dg, int mdRow, string strSQL = "",bool singleSelectionMode = true, bool hideRevisions = false, bool onlySOPCodes = false)
        {

            int CurrentUserSecurityLevel = Properties.Settings.Default.CurrentUserSecurityLevel;
            string CurrentUserName = Properties.Settings.Default.CurrentUsername;

            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;

            if (strSQL == "")
            {
                // Only load documents that comply with the user's security level.
                strSQL = "SELECT DocumentMaster.*, Departments.DepartmentName, ERPCustomerDocumentLinks.CustomerName, ERPVendorDocumentLinks.VendorName " +
                    "FROM DocumentMaster " +
                    "LEFT OUTER JOIN Departments ON DocumentMaster.DepartmentID = Departments.ID " +
                    "LEFT OUTER JOIN ERPCustomerDocumentLinks ON DocumentMaster.ID = ERPCustomerDocumentLinks.DocumentID " +
                    "LEFT OUTER JOIN ERPVendorDocumentLinks ON DocumentMaster.ID = ERPVendorDocumentLinks.DocumentID "; 

                switch (CurrentUserSecurityLevel)
                {
                    case SecurityLevel.ReadOnly:
                        // Documents shared to this user
                        strSQL += " WHERE DocumentMaster.ID IN (SELECT DocumentID FROM SharedDocuments WHERE SharedTo = '" + CurrentUserName + "') ";
                        // Documents created by this user.
                        strSQL += " OR DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        // Display all public documents.
                        strSQL += " OR DocumentMaster.IsPublic = 1 ";
                        // Documents shared to this user
                        //strSQL += " OR '" + CurrentUserName + "' IN (SELECT SharedTo FROM SharedDocuments) ";
                        // Documents that are part of a department the user belongs to.
                        strSQL += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        // Do not display private documents created by other users.
                        strSQL += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1)";
                        break;
                    case SecurityLevel.SystemAdmin:
                        // Do not display private documents created by other users.
                        strSQL += " WHERE NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1)";
                        // If the "Hide Revisions" checkbox is checked on the main page then hide all revised documents.
                        if (hideRevisions)
                        {
                            strSQL += "AND IsDeprecated = 0 ";
                        }
                        if (onlySOPCodes)
                        {
                            strSQL += "AND ISOType IN ('QC', 'SOP', 'FORM', 'MAPPING', 'PROCESS', 'RECORDS', 'STANDARD') ";
                        }
                        break;
                    case SecurityLevel.GroupAdmin:
                        // Documents shared to this user
                        strSQL += " WHERE DocumentMaster.ID IN (SELECT DocumentID FROM SharedDocuments WHERE SharedTo = '" + CurrentUserName + "') ";
                        // Documents created by this user.
                        strSQL += " OR (DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        //strSQL += " WHERE (DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        // Display all public documents.
                        strSQL += " OR DocumentMaster.IsPublic = 1 ";
                        // Do not display private documents created by other users.
                        strSQL += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1)";
                        // Documents that are part of a department the user belongs to.
                        strSQL += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        // Documents that are part of a department the user belongs to.
                        strSQL += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM AdminDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0)) ";
                        if (hideRevisions)
                        {
                            strSQL += "AND IsDeprecated = 0 ";
                        }
                        if (onlySOPCodes)
                        {
                            strSQL += "AND ISOType IN ('QC', 'SOP', 'FORM', 'MAPPING', 'PROCESS', 'RECORDS', 'STANDARD') ";
                        }

                        break;
                    case SecurityLevel.GroupUser:
                        // Documents shared to this user
                        strSQL += " WHERE DocumentMaster.ID IN (SELECT DocumentID FROM SharedDocuments WHERE SharedTo = '" + CurrentUserName + "') ";
                        // Documents created by this user.
                        strSQL += " OR DocumentMaster.CreatedBy = " + chr39 + CurrentUserName + chr39 + " ";
                        // Display all public documents.
                        strSQL += " OR DocumentMaster.IsPublic = 1 ";
                        // Documents shared to this user
                        //strSQL += " OR '" + CurrentUserName + "' IN (SELECT SharedTo FROM SharedDocuments) ";
                        // Documents that are part of a department the user belongs to.
                        strSQL += " OR DocumentMaster.DepartmentID IN (SELECT DepartmentID FROM UserDepartments " +
                            "WHERE UserName = " + chr39 + CurrentUserName + chr39 + " AND IsPrivate = 0) ";
                        // Do not display private documents created by other users.
                        strSQL += " AND NOT (DocumentMaster.CreatedBY <> " + chr39 + CurrentUserName + chr39 +
                            " AND DocumentMaster.IsPrivate = 1)";
                        break;
                }

                //MessageBox.Show(strSQL);

                strSQL += "ORDER BY DocumentMaster.ID DESC";
            }

            cmd.CommandText = strSQL;

            ds.Clear();

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            dg.ItemsSource = null;
            dg.ItemsSource = ds.Tables[0].DefaultView;

            ConfigureDataGridOptions(dg,true);

            if (!singleSelectionMode)
            {
                dg.SelectionMode = (DataGridSelectionMode) SelectionMode.Multiple;
            }

            switch (CurrentUserSecurityLevel)
            {
                //case SecurityLevel.ReadOnly:
                //    //dg.Columns[1].Visibility = Visibility.Hidden;
                //    //dg.Columns[2].Visibility = Visibility.Hidden;
                //    //dg.Columns[3].Visibility = Visibility.Hidden;
                //    //dg.Columns[4].Visibility = Visibility.Hidden;
                //    dg.Columns[10].Visibility = Visibility.Hidden;
                //    dg.Columns[11].Visibility = Visibility.Hidden;
                //    dg.Columns[12].Visibility = Visibility.Hidden;
                //    dg.Columns[13].Visibility = Visibility.Hidden;
                //    //dg.Columns[dg.Columns.Count - 1].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                //    break;
            }

            //dg.Columns[0].Visibility = Visibility.Hidden;

            dg.Columns[1].Header = "ISO\nDocument\nType";
            dg.Columns[2].Header = "Tier\nLevel";
            dg.Columns[3].Header = "ISO\nTag";
            dg.Columns[4].Header = "Revision\nInfo";
            dg.Columns[5].Header = "Document\nTitle";
            dg.Columns[6].Header = "Transaction\nDate";
            dg.Columns[7].Header = "Transaction\nAmount";
            dg.Columns[8].Header = "ISO\nEffective\nDate";
            dg.Columns[9].Header = "Document\nType";
            dg.Columns[10].Header = "Date\nCreated";
            dg.Columns[11].Header = "Created\nBy";
            dg.Columns[12].Header = "Date\nModified";
            dg.Columns[13].Header = "Modified\nBy";

            dg.Columns[6].Visibility = Visibility.Hidden;
            dg.Columns[7].Visibility = Visibility.Hidden;
            dg.Columns[8].Visibility = Visibility.Hidden;

            for (int x = 14; x <=22; x++)
            {
                dg.Columns[x].Visibility = Visibility.Hidden;
            }

            dg.Columns[5].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            SelectDGGridRowByIndex(dg, mdRow);
        }


        public void ConfigureDataGridOptions(DataGrid dg,bool canSort = false, DataSet ds=null, int row = -1, int column = -1)
        {
            if (ds != null)
            {
                dg.ItemsSource = ds.Tables[0].DefaultView;
            }

            dg.RowHeaderWidth = 0;
            dg.CanUserAddRows = false;
            dg.CanUserDeleteRows = false;
            dg.CanUserReorderColumns = false;
            dg.CanUserSortColumns = canSort;
            dg.IsReadOnly = true;
            dg.SelectionUnit = DataGridSelectionUnit.FullRow;
            dg.SelectionMode = DataGridSelectionMode.Single;

            if (row >= 0)
            {
                SelectDGGridRowByIndex(dg, row);
            }

            if (column >= 0)
            {
                dg.Columns[column].Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

        }

        public string GetOdysseyEmployeeName(string employeeID)
        {
            string empName = "";

            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmd = new OdbcCommand();
            cmd.Connection = buck.cnOdyssey;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT Name FROM Employees WHERE PayrollID = '" + employeeID + "'";

            empName = cmd.ExecuteScalar().ToString();
            buck.DBCloseOdysseyDatabase();

            return empName;
        }

        public object GetOdysseyVendorName(string vendorID)
        {
            object vendorName = "";

            IniFile ini = new IniFile(@"C:\Temp\ISO-DMS.ini");

            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = ini.ReadValue("Database", "ConnectionString_SLDB01");
            cn.Open();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT name FROM vendaddr_mst WHERE LTRIM(RTRIM(vend_num)) = '" + vendorID + "'";

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            cn.Close();

            if (ds.Tables[0].Rows.Count > 0)
            {
                vendorName = ds.Tables[0].Rows[0]["name"].ToString();
            }

            return vendorName;
        }

        public object GetOdysseyCustomerName(string customerID) 
        {
            object customerName = "";

            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmd = new OdbcCommand();
            cmd.Connection = buck.cnOdyssey;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT CoName FROM Customer WHERE Customer = " + chr39 + customerID + chr39 ;

            DataSet ds = new DataSet();

            OdbcDataAdapter da = new OdbcDataAdapter(cmd);
            da.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                customerName = ds.Tables[0].Rows[0]["CoName"].ToString();
            }

            buck.DBCloseOdysseyDatabase();
            return customerName;
        }

        public object GetOdysseyProductDescription(string productID)
        {
            object productDescription = "";

            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmd = new OdbcCommand();
            cmd.Connection = buck.cnOdyssey;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT CustPart FROM Products WHERE Product = " + chr39 + productID + chr39;

            DataSet ds = new DataSet();

            OdbcDataAdapter da = new OdbcDataAdapter(cmd);
            da.Fill(ds);

            if (ds.Tables[0].Rows.Count > 0)
            {
                productDescription = ds.Tables[0].Rows[0]["CustPart"].ToString();
            }

            buck.DBCloseOdysseyDatabase();
            return productDescription;
        }

        public string getNextDMSFileName()
        {
            int fileNameCounter = 0;
            int folderLevel1Counter = 0;
            int folderLevel2Counter = 0;
            int folderLevel3Counter = 0;

            string fileName = "";

            DBOpenSQLDB();
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT * FROM SystemFolderCounters";

            DataSet ds = new DataSet();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            fileNameCounter = (int)ds.Tables[0].Rows[0]["FileNameCounter"] + 1;
            folderLevel1Counter = (int)ds.Tables[0].Rows[0]["FolderLevel1Counter"];
            folderLevel2Counter = (int)ds.Tables[0].Rows[0]["FolderLevel2Counter"];
            folderLevel3Counter = (int)ds.Tables[0].Rows[0]["FolderLevel3Counter"];

            if (fileNameCounter > 255)
            {
                // If the number of files exceeds 256 then increase the level3 folder number by 1 
                // and reset the fileNumberCounter to 1
                fileNameCounter = 0;
                folderLevel3Counter += 1;

                // If the folderLevel3Counter exceeds 256 then increase the level2 folder by 1
                // and reset the level3 folder to 0
                if (folderLevel3Counter > 255)
                {
                    folderLevel3Counter = 0;
                    folderLevel2Counter += 1;
                }

                // If the folderLevel2Counter exceeds 256 then increase the level1 folder by 1
                // and reset the level2 folder to 0
                if (folderLevel2Counter > 255)
                {
                    folderLevel2Counter = 0;
                    folderLevel1Counter += 1;
                }
            }

            string level1Folder = folderLevel1Counter.ToString("X2");
            string level2Folder = folderLevel2Counter.ToString("X2");
            string level3Folder = folderLevel3Counter.ToString("X2");

            fileName = level1Folder;
            fileName += level2Folder;
            fileName += level3Folder;
            fileName += fileNameCounter.ToString("X2");

            // Make sure the proper folders exist
            if (!Directory.Exists(ControlledDocPath + level1Folder))
            {
                Directory.CreateDirectory(ControlledDocPath + level1Folder);
                Directory.CreateDirectory(UncontrolledDocPath + level1Folder);
                Directory.CreateDirectory(DeletedDocPath + level1Folder);
            }

            if (!Directory.Exists(ControlledDocPath + level1Folder + @"\" + level2Folder))
            {
                Directory.CreateDirectory(ControlledDocPath + level1Folder + @"\" + level2Folder);
                Directory.CreateDirectory(UncontrolledDocPath + level1Folder + @"\" + level2Folder);
                Directory.CreateDirectory(DeletedDocPath + level1Folder + @"\" + level2Folder);
            }

            if (!Directory.Exists(ControlledDocPath + level1Folder + @"\" + level2Folder + @"\" + level3Folder))
            {
                Directory.CreateDirectory(ControlledDocPath + level1Folder + @"\" + level2Folder + @"\" + level3Folder);
                Directory.CreateDirectory(UncontrolledDocPath + level1Folder + @"\" + level2Folder + @"\" + level3Folder);
                Directory.CreateDirectory(DeletedDocPath + level1Folder + @"\" + level2Folder + @"\" + level3Folder);
            }

            // Update the system table
            cmd.CommandText = "UPDATE SystemFolderCounters SET FolderLevel1Counter = " + folderLevel1Counter.ToString() + ", "
                + "FolderLevel2Counter = " + folderLevel2Counter.ToString() + ", FolderLevel3Counter = " + folderLevel3Counter.ToString() + ", "
                + "FileNameCounter = " + fileNameCounter.ToString();
            cmd.ExecuteNonQuery();

            buck.DBCloseDatabase();

            return level1Folder + @"\" + level2Folder + @"\" + level3Folder + @"\" + fileName;
        }

        public DataSet DBCreateDataSet(string sql)
        {
            DataSet ds = new DataSet();

            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;

            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(ds);

            buck.DBCloseDatabase();

            return ds;
        }


        public DataSet DBCreateODBCDataSet(string sql)
        {
            DataSet ds = new DataSet();

            buck.DBOpenOdysseyDatabase(false);

            OdbcCommand cmd = new OdbcCommand();
            cmd.Connection = buck.cnOdyssey;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;

            OdbcDataAdapter da = new OdbcDataAdapter(cmd);
            da.Fill(ds);

            buck.DBCloseDatabase();

            return ds;
        }

        public int DBExecuteScalar(string sql)
        {
            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;

            int result = (int)cmd.ExecuteScalar();

            buck.DBCloseDatabase();

            return result;
        }

        public void DBExecuteNonQuery(string sql)
        {
            DBOpenSQLDB();

            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnSQLDB;
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            buck.DBCloseDatabase();
        }
    }




}
