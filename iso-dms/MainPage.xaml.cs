using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwainScanning;
using TwainScanning.Collectors;
using TwainScanning.NativeStructs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : System.Windows.Controls.Page
    {
        public Char chr39 = Convert.ToChar(39);
        public string documentSearchSQLText = "";
        public bool documentRevisionInProgress = false;

        public static bool newDocumentFound = false;

        public bool scanningCancelled = false;
        private bool editingDocument = false;

        private int totalDocumentsCount = 0;

        ISO_DMS.Tools tools = new Tools();

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        BuckUtils.Msg msg = new BuckUtils.Msg();

        IniFile ini = new IniFile(@"C:\Temp\ISO-DMS.ini");

        DispatcherTimer MainPageTimer = new DispatcherTimer();
        bool blnLoading = true;

        DispatcherTimer PageReloadTimer = new DispatcherTimer();

        string DeletedDocPath = @"C:\Earl\ISO\Documents\Deleted\";
        private string currentUser = "";

        public static DataSet dsMasterDocs = new DataSet();

        int currentMasterDocsGridRow = 0;

        // Set button animation aspects for notifying the user that others have added documents.
        // THe refresh button will flash until clicked (handled by PageReloadTimer_Tick)
        System.Windows.Media.Animation.ColorAnimation animation = new System.Windows.Media.Animation.ColorAnimation();

        public MainPage()
        {
            InitializeComponent();
            ini.WriteValue("Company", "CompanyName", "Travis Body & Trailers, Inc.");
            ini.WriteValue("Company", "ProgramTitle", "Travis DMS");

            ini.WriteValue("FilePaths", "DocumentPath_Controlled", @"C:\Earl\ISO\Documents\Controlled");
            ini.WriteValue("FilePaths", "DocumentPath_UnControlled", @"C:\Earl\ISO\Documents\Uncontrolled");
            ini.WriteValue("FilePaths", "DocumentPath_Deleted", @"C:\Earl\ISO\Documents\Deleted");
            ini.WriteValue("Database", "ServerName", "SLDB02");
            ini.WriteValue("Database", "DatabaseName", "ISO-DMS");
            ini.WriteValue("Database", "ConnectionString_SLDB02", "SERVER=SLDB02;DATABASE=ISO-DMS;TRUSTED_CONNECTION=YES;INTEGRATED SECURITY = SSPI; MultipleActiveResultSets=True");
            ini.WriteValue("Database", "ConnectionString_SLDB01", "SERVER=SLDB01;DATABASE=Corp_App;TRUSTED_CONNECTION=YES;INTEGRATED SECURITY = SSPI; MultipleActiveResultSets=True");

            textBlockCompanyName.Text = ini.ReadValue("Company", "CompanyName") + "\nDocument Management System";
        }

        private int GetMasterDocumentsCount()
        {
            string sql = "SELECT COUNT(*) FROM DocumentMaster";
            return tools.DBExecuteScalar(sql);
        }

        private void PageReloadTimer_Tick(object sender, EventArgs e)
        {
            
            // Alert the user that there are new documents in the InBox.
            int inBoxCount = tools.DBExecuteScalar("SELECT COUNT (*) FROM Inbox WHERE Recipient = '" + currentUser + "' AND MessageViewed = 0");
            buck.DoEvents();

            if (inBoxCount > 0)
            {
                this.btnInBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                this.btnInBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }

            // Alert the user that there are documents waiting to send in the OutBox.
            int outBoxCount = tools.DBExecuteScalar("SELECT COUNT (*) FROM OutBoxQueuedDocuments WHERE Sender = '" + currentUser + "'");
            buck.DoEvents();

            if (outBoxCount > 0)
            {
                this.btnOutBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);
            }
            else
            {
                this.btnOutBox.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
            }

            if (totalDocumentsCount != GetMasterDocumentsCount())
            {
                int docCount = totalDocumentsCount;
                totalDocumentsCount = GetMasterDocumentsCount();

                if (docCount > 0)
                {
                    if (!Properties.Settings.Default.documentCountUpdatedByCurrentUser)
                    {
                        //this.btnRefresh.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);

                        Properties.Settings.Default.documentCountUpdatedByCurrentUser = false;
                        Properties.Settings.Default.Save();
                    }
                }
            }
            else
            {
                if (Properties.Settings.Default.cancelRefreshMasterDocuments)
                {
                    Properties.Settings.Default.cancelRefreshMasterDocuments = false;
                    Properties.Settings.Default.Save();
                    //this.btnRefresh.Background.BeginAnimation(SolidColorBrush.ColorProperty, null);
                }
            }
        }

        private void MainPageTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                if (blnLoading)
                {
                    tools.CheckUserCredentials(buck.GetCurrentUserName());
                    dgMasterDocs.Focus();
                    blnLoading = false;
                }

                if (newDocumentFound == true)
                {
                    MainPage.newDocumentFound = false;
                    string fileName = Properties.Settings.Default.monitoredDocumentName;

                    string docType = "";

                    switch (System.IO.Path.GetExtension(fileName).ToUpper())
                    {
                        case ".JPG":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case ".JPEG":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case ".GIF":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case ".BMP":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case ".PNG":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case "*.TIFF":
                            docType = "Image";
                            CheckInImageDocument(fileName, fileName, docType);
                            break;
                        case ".PDF":
                            docType = "PDF";
                            CheckInPDFDocument(fileName, fileName, docType);
                            break;
                        case ".DOCX":
                            docType = "WORD";
                            CheckInWordDocument(fileName, fileName, docType);
                            break;
                        case ".DOC":
                            docType = "WORD";
                            CheckInWordDocument(fileName, fileName, docType);
                            break;
                        case ".XLSX":
                            docType = "EXCEL";
                            CheckInExcelDocument(fileName, fileName, docType);
                            break;
                        case ".XLS":
                            docType = "EXCEL";
                            CheckInExcelDocument(fileName, fileName, docType);
                            break;
                        case ".PPTX":
                            docType = "POWERPOINT";
                            CheckInPowerPointDocument(fileName, fileName, docType);
                            break;
                        case ".PPT":
                            docType = "POWERPOINT";
                            CheckInPowerPointDocument(fileName, fileName, docType);
                            break;
                    }
                    try { File.Delete(fileName); } catch { }
                }
            }
        }

        private void btnCheckIn_Click(object sender, RoutedEventArgs e)
        {
            CheckInFilePicker();
            Properties.Settings.Default.documentSearchInProgress = false;
            Properties.Settings.Default.Save();
        }

        private void CheckInFilePicker()
        {
            MainPage.newDocumentFound = false;

            if (Properties.Settings.Default.LastCheckInDocumentsPath.ToString().Length == 0)
            {
                Properties.Settings.Default.LastCheckInDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Properties.Settings.Default.Save();
            }

            Word.Application wordApplication = new Word.Application();
            object paramMissing = Type.Missing;

            OpenFileDialog filePicker = new OpenFileDialog();
            filePicker.Title = "Select File To Be Checked In:";
            filePicker.InitialDirectory = Properties.Settings.Default.LastCheckInDocumentsPath;

            if (filePicker.ShowDialog() == true)
            {

                // Save the last directory selected by the user
                Properties.Settings.Default.LastCheckInDocumentsPath = System.IO.Path.GetDirectoryName(filePicker.FileName);
                Properties.Settings.Default.Save();

                // This list contains image file extensions.
                List<string> imageFileExtensions = new List<string> { ".JPG", ".JPEG", ".GIF", ".BMP", ".PNG", ".TIFF" };

                string fileExtension = System.IO.Path.GetExtension(filePicker.SafeFileName.ToString());

                string docType = "";

                switch (fileExtension.ToUpper())
                {
                    case ".JPG":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".JPEG":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".GIF":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".BMP":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".PNG":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case "*.TIFF":
                        docType = "Image";
                        CheckInImageDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".PDF":
                        docType = "PDF";
                        CheckInPDFDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".DOCX":
                        docType = "WORD";
                        CheckInWordDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".DOC":
                        docType = "WORD";
                        CheckInWordDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".XLSX":
                        docType = "EXCEL";
                        CheckInExcelDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".XLS":
                        docType = "EXCEL";
                        CheckInExcelDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".PPTX":
                        docType = "POWERPOINT";
                        CheckInPowerPointDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    case ".PPT":
                        docType = "POWERPOINT";
                        CheckInPowerPointDocument(filePicker.SafeFileName, filePicker.FileName, docType);
                        break;
                    default:
                        MessageBox.Show("You have selected an unsupported document type.\n\nCurrently only Word, Excel, PowerPoint and PDF are supported.", "Notice");
                        Properties.Settings.Default.RevisonCheckedIn = false;
                        Properties.Settings.Default.Save();
                        break;
                }

                EditDocumentProperties();

                if (dsMasterDocs.Tables[0].Rows.Count > 0)
                {
                    if ((int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["DepartmentID"] == tools.HelpDepartmentID)
                    {
                        AddDocumentToApplication();
                    }
                }
                else
                {
                    AddDocumentToApplication();
                }
            }
        }

        private void CheckInImageDocument(string dlgSafeFileName, string dlgFileName, string docType)
        {

            MsgBox ShowMsg = new MsgBox("Checking in selected image document ...please stand by.");
            ShowMsg.Show();
            buck.DoEvents();

            string fileExtension = System.IO.Path.GetExtension(dlgFileName);
            string fileName = tools.getNextDMSFileName();
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(dlgFileName);

            string paramExportUncontrolledFilePath = tools.UncontrolledDocPath + fileName + ".pdf";

            string paramExportControlledFilePath = tools.ControlledDocPath + fileName + fileExtension;
            string paramSourceDocPath = dlgFileName.ToString();

            PdfDocument pdfDoc = new PdfDocument();
            PdfPage oPage = new PdfPage();
            //pdfDoc.Pages.Add(new PdfPage());
            pdfDoc.Pages.Add(oPage);


            XGraphics xgr = XGraphics.FromPdfPage(pdfDoc.Pages[0]);
            XImage img = XImage.FromFile(paramSourceDocPath);

            oPage.Height = img.PixelHeight;
            oPage.Width = img.PixelWidth;

            xgr.DrawImage(img, 0, 0, oPage.Width, oPage.Height);

            pdfDoc.Save(paramExportUncontrolledFilePath);
            pdfDoc.Close();

            Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
            Properties.Settings.Default.Save();

            tools.DBInsertMasterDocument(dlgFileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);

            int newDocID = (int)dsMasterDocs.Tables[0].Rows[dsMasterDocs.Tables[0].Rows.Count - 1]["ID"];
            tools.WriteSecurityLogEntry(newDocID, tools.logEvent_CheckedDocumentIn, dlgSafeFileName);
            ShowMsg.Close();
        }

        private void CheckInPowerPointDocument(string dlgSafeFileName, string dlgFileName, string docType)
        {
            Microsoft.Office.Core.MsoTriState ofalse = Microsoft.Office.Core.MsoTriState.msoFalse;
            //Microsoft.Office.Core.MsoTriState otrue = Microsoft.Office.Core.MsoTriState.msoTrue;

            MsgBox ShowMsg = new MsgBox("Checking in selected PowerPoint document ...please stand by.");
            ShowMsg.Show();
            buck.DoEvents();

            PowerPoint.Application ppApp = new PowerPoint.Application();
            //ppApp.Visible = ofalse;
            //ppApp.Activate();

            PowerPoint.Presentations ppPres = ppApp.Presentations;
            object paramMissing = Type.Missing;

            string fileExtension = System.IO.Path.GetExtension(dlgFileName);
            string fileName = tools.getNextDMSFileName();
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(dlgFileName);

            string paramExportControlledFilePath = tools.ControlledDocPath + fileName + fileExtension;
            string paramExportUncontrolledFilePath = tools.UncontrolledDocPath + fileName + ".pdf";
            string paramSourceDocPath = dlgFileName.ToString();

            PowerPoint.Presentation ppDoc = ppPres.Open(paramSourceDocPath, ofalse, ofalse, ofalse);

            ppDoc.SaveAs(paramExportUncontrolledFilePath, PpSaveAsFileType.ppSaveAsPDF);

            Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
            Properties.Settings.Default.Save();

            tools.DBInsertMasterDocument(dlgFileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);
            int newDocID = (int)dsMasterDocs.Tables[0].Rows[dsMasterDocs.Tables[0].Rows.Count - 1]["ID"];
            tools.WriteSecurityLogEntry(newDocID, tools.logEvent_CheckedDocumentIn, dlgSafeFileName);
            ShowMsg.Close();
        }


        private void CheckInWordDocument(string dlgSafeFileName, string dlgFileName, string docType)
        {
            MsgBox ShowMsg = new MsgBox("Checking in selected Word document ...please stand by.");
            ShowMsg.Show();
            buck.DoEvents();

            Word.Application wordApplication = new Word.Application();
            Word.Document wordDocument = null;
            object paramMissing = Type.Missing;

            string fileExtension = System.IO.Path.GetExtension(dlgFileName);
            string fileName = tools.getNextDMSFileName();
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(dlgFileName);

            //string paramExportControlledFilePath = ControlledDocPath + @"Word\" + dlgSafeFileName;
            string paramExportControlledFilePath = tools.ControlledDocPath + fileName + fileExtension;

            //string paramExportUncontrolledFilePath = System.IO.Path.ChangeExtension(UncontrolledDocPath + @"Word\" + dlgSafeFileName, ".pdf");
            string paramExportUncontrolledFilePath = tools.UncontrolledDocPath + fileName + ".pdf";

            object paramSourceDocPath = dlgFileName.ToString();
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
                    ref paramSourceDocPath, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing, ref paramMissing, ref paramMissing,
                    ref paramMissing);

                // Export it in the specified format.
                if (wordDocument != null)
                    wordDocument.ExportAsFixedFormat(paramExportUncontrolledFilePath,
                        paramExportFormat, paramOpenAfterExport,
                        paramExportOptimizeFor, paramExportRange, paramStartPage,
                        paramEndPage, paramExportItem, paramIncludeDocProps,
                        paramKeepIRM, paramCreateBookmarks, paramDocStructureTags,
                        paramBitmapMissingFonts, paramUseISO19005_1,
                        ref paramMissing);

                Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
                Properties.Settings.Default.Save();

                tools.DBInsertMasterDocument(dlgFileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);
                int newDocID = (int)dsMasterDocs.Tables[0].Rows[dsMasterDocs.Tables[0].Rows.Count - 1]["ID"];
                tools.WriteSecurityLogEntry(newDocID, tools.logEvent_CheckedDocumentIn, dlgSafeFileName);
            }
            catch
            {

            }
            finally
            {
                // Close and release the Document object.
                if (wordDocument != null)
                {
                    wordDocument.Close(ref paramMissing, ref paramMissing,
                        ref paramMissing);
                    wordDocument = null;
                }

                // Quit Word and release the ApplicationClass object.
                if (wordApplication != null)
                {
                    wordApplication.Quit(ref paramMissing, ref paramMissing,
                        ref paramMissing);
                    wordApplication = null;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                ShowMsg.Close();
            }
        }

        private void CheckInScannedPDFDocument(string fileName, string docType)
        {
            MsgBox ShowMsg = new MsgBox("Checking in scanned PDF ...please stand by.");
            ShowMsg.Show();
            buck.DoEvents();

            string shortFileName = System.IO.Path.GetFileName(fileName);

            string paramExportControlledFilePath = tools.UncontrolledDocPath + shortFileName;
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(paramExportControlledFilePath);

            string paramExportUncontrolledFilePath = paramExportControlledFilePath;

            Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
            Properties.Settings.Default.Save();

            tools.DBInsertMasterDocument(fileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);

            try { File.Delete(fileName); } catch { }
            ShowMsg.Close();
        }

        private void CheckInPDFDocument(string dlgSafeFileName, string dlgFileName, string docType)
        {
            MsgBox ShowMsg = new MsgBox("Checking in selected PDF ...please stand by.");
            ShowMsg.Show();

            buck.DoEvents();

            string fileExtension = System.IO.Path.GetExtension(dlgFileName);
            string fileName = tools.getNextDMSFileName();
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(dlgFileName);

            string paramExportControlledFilePath = tools.UncontrolledDocPath + fileName + fileExtension;
            string paramExportUncontrolledFilePath = tools.UncontrolledDocPath + fileName + fileExtension;

            Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
            Properties.Settings.Default.Save();

            tools.DBInsertMasterDocument(dlgFileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);
            ShowMsg.Close();
        }

        private void CheckInExcelDocument(string dlgSafeFileName, string dlgFileName, string docType)
        {

            MsgBox ShowMsg = new MsgBox("Checking in selected Excel workbook ...please stand by.");
            ShowMsg.Show();
            buck.DoEvents();

            Excel.Application excelApplication = new Excel.Application();
            object paramMissing = Type.Missing;

            string fileExtension = System.IO.Path.GetExtension(dlgFileName);
            string fileName = tools.getNextDMSFileName();
            string docTitle = System.IO.Path.GetFileNameWithoutExtension(dlgFileName);

            string paramExportControlledFilePath = tools.ControlledDocPath + fileName + fileExtension;
            string paramExportUncontrolledFilePath = tools.UncontrolledDocPath + fileName + ".pdf";
            object paramSourceDocPath = dlgFileName.ToString();

            // Open the source document.
            Workbook workBook = excelApplication.Workbooks.Open(dlgFileName,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                    Type.Missing, Type.Missing);

            Properties.Settings.Default.RevistedDocumentPath = paramExportControlledFilePath;
            Properties.Settings.Default.Save();

            tools.DBInsertMasterDocument(dlgFileName, docTitle, paramExportControlledFilePath, paramExportUncontrolledFilePath, docType, dsMasterDocs, dgMasterDocs);
            int newDocID = (int)dsMasterDocs.Tables[0].Rows[dsMasterDocs.Tables[0].Rows.Count - 1]["ID"];
            tools.WriteSecurityLogEntry(newDocID, tools.logEvent_CheckedDocumentIn, dlgSafeFileName);
            workBook.Close(true);
            excelApplication.Quit();
            workBook = null;
            excelApplication = null;
            ShowMsg.Close();
        }

        private void SetUserProgramRights(string userName = "")
        {
            if (userName.Length == 0)
            {
                userName = buck.GetCurrentUserName();
            }

            App.Current.MainWindow.Title = ini.ReadValue("Company","ProgramTitle");

            Properties.Settings.Default.CurrentUserSecurityLevel = tools.GetUserSecurityLevel(userName);
            Properties.Settings.Default.CurrentUsername = userName;
            Properties.Settings.Default.Save();

            this.chkHideRevisedVersions.IsChecked = Properties.Settings.Default.HideRevisions;

            // Get the user's security level and disable buttons as needed.
            switch (Properties.Settings.Default.CurrentUserSecurityLevel)
            {
                // Read Only access
                case SecurityLevel.ReadOnly:
                    txtBlockSecurityLevel.Text = "Current User = " + userName + "\nSecurity Level = Read Only";
                    btnUpdateRevision.Visibility = Visibility.Hidden;
                    btnCheckIn.Visibility = Visibility.Hidden;
                    btnDelete.Visibility = Visibility.Hidden;
                    btnEditProperties.Visibility = Visibility.Hidden;
                    btnEmailUncontrolled.Visibility = Visibility.Hidden;
                    btnSetup.Visibility = Visibility.Hidden;
                    btnScanIn.Visibility = Visibility.Hidden;
                    btnHelp.Visibility = Visibility.Hidden;
                    btnCaptureFromClipboard.Visibility = Visibility.Hidden;
                    btnDepartmentModules.Visibility = Visibility.Hidden;
                    btnNotes.Visibility = Visibility.Hidden;
                    chkHideRevisedVersions.Visibility = Visibility.Hidden;
                    chkShowISODocumentsOnly.Visibility = Visibility.Hidden;
                    chkPrintAll.Visibility = Visibility.Hidden;
                    cMenuMasterDocs.Visibility = Visibility.Hidden;
                    chkContinuousScanning.Visibility = Visibility.Hidden;
                    btnInBox.Visibility = Visibility.Hidden;
                    btnOutBox.Visibility = Visibility.Hidden;

                    // Resize the grid to use the space from the invisible buttons.
                    int gridTop = (int)dgMasterDocs.Margin.Top;
                    int buttonTop = (int)dgMasterDocs.Margin.Top;

                    Thickness thickness = new Thickness();
                    thickness.Left = dgMasterDocs.Margin.Left;
                    thickness.Top = btnCheckIn.Margin.Top;
                    dgMasterDocs.Margin = thickness;
                    dgMasterDocs.Height += btnCheckIn.Height;

                    // Hide the Red and Yellow document explanations at the bottom of the grid.
                    lblRed.Visibility = Visibility.Hidden;
                    lblRedDescription.Visibility = Visibility.Hidden;
                    lblYellow.Visibility = Visibility.Hidden;
                    lblYellowDescription.Visibility = Visibility.Hidden;

                    break;
                // System Admin rights
                case SecurityLevel.SystemAdmin:

                    ConfigureAdminButtons();

                    txtBlockSecurityLevel.Text = "Current User = " + userName + "\nSecurity Level = System Administrator";
                    btnSetup.Visibility = Visibility.Visible;
                    break;
                // Group Admin rights
                case SecurityLevel.GroupAdmin:

                    ConfigureAdminButtons();

                    txtBlockSecurityLevel.Text = "Current User = " + userName + "\nSecurity Level = Group Adminisrator";
                    btnSetup.Visibility = Visibility.Hidden;
                    chkPrintAll.Visibility = Visibility.Hidden;
                    chkContinuousScanning.Visibility = Visibility.Hidden;
                    btnDepartmentModules.Visibility = Visibility.Hidden;
                    break;
                // Group User
                case SecurityLevel.GroupUser:

                    ConfigureAdminButtons();

                    txtBlockSecurityLevel.Text = "Current User = " + userName + "\nSecurity Level = Group User";
                    btnUpdateRevision.Visibility = Visibility.Hidden;
                    btnSetup.Visibility = Visibility.Hidden;
                    btnDelete.Visibility = Visibility.Hidden;
                    btnCaptureFromClipboard.Visibility = Visibility.Hidden;
                    chkHideRevisedVersions.Visibility = Visibility.Hidden;
                    cMenuMasterDocs.Visibility = Visibility.Hidden;
                    chkContinuousScanning.Visibility = Visibility.Hidden;
                    chkPrintAll.Visibility = Visibility.Hidden;
                    btnDepartmentModules.Visibility = Visibility.Hidden;
                    break;
            }

            if (userName.Length > 0) { RefreshDocuments(); }
        }

        private void ConfigureAdminButtons()
        {
            btnUpdateRevision.Visibility = Visibility.Visible;
            btnCheckIn.Visibility = Visibility.Visible;
            btnDelete.Visibility = Visibility.Visible;
            btnEditProperties.Visibility = Visibility.Visible;
            btnEmailUncontrolled.Visibility = Visibility.Visible;
            btnSetup.Visibility = Visibility.Visible;
            btnScanIn.Visibility = Visibility.Visible;
            btnHelp.Visibility = Visibility.Visible;
            chkHideRevisedVersions.Visibility = Visibility.Visible;
            chkShowISODocumentsOnly.Visibility = Visibility.Visible;
            cMenuMasterDocs.Visibility = Visibility.Visible;
            btnCaptureFromClipboard.Visibility = Visibility.Visible;
            btnDepartmentModules.Visibility = Visibility.Visible;
            btnNotes.Visibility = Visibility.Visible;
            chkContinuousScanning.Visibility = Visibility.Visible;
            btnInBox.Visibility = Visibility.Visible;
            btnOutBox.Visibility = Visibility.Visible;

            // Show the Red and Yellow document explanations at the bottom of the grid.
            lblRed.Visibility = Visibility.Visible;
            lblRedDescription.Visibility = Visibility.Visible;
            lblYellow.Visibility = Visibility.Visible;
            lblYellowDescription.Visibility = Visibility.Visible;

            Thickness dgthickness = new Thickness();
            dgthickness.Left = Properties.Settings.Default.GridLeft;
            dgthickness.Top = Properties.Settings.Default.GridTop;
            dgMasterDocs.Margin = dgthickness;
            dgMasterDocs.Height = Properties.Settings.Default.GridHeight;
        }

        // Define the event handlers.
        public static void watcher_OnChanged(object source, FileSystemEventArgs e)
        {
            // Specify what is done when a file is changed.
            string shortFileName1stCharacter = System.IO.Path.GetFileName(e.FullPath).ToString().Substring(0, 1);

            if (shortFileName1stCharacter != "~")
            {
                string docType = "";
                string fileExtension = System.IO.Path.GetExtension(e.FullPath).ToUpper();
                switch (fileExtension)
                {
                    case ".JPG":
                        docType = "Image";
                        break;
                    case ".JPEG":
                        docType = "Image";
                        break;
                    case ".GIF":
                        docType = "Image";
                        break;
                    case ".BMP":
                        docType = "Image";
                        break;
                    case ".PNG":
                        docType = "Image";
                        break;
                    case "*.TIFF":
                        docType = "Image";
                        break;
                    case ".PDF":
                        docType = "PDF";
                        break;
                    case ".DOCX":
                        docType = "WORD";
                        break;
                    case ".DOC":
                        docType = "WORD";
                        break;
                    case ".XLSX":
                        docType = "EXCEL";
                        break;
                    case ".XLS":
                        docType = "EXCEL";
                        break;
                    case ".PPTX":
                        docType = "POWERPOINT";
                        break;
                    case ".PPT":
                        docType = "POWERPOINT";
                        break;
                }

                if (docType.Length > 0)
                {
                    Properties.Settings.Default.monitoredDocumentName = e.FullPath;
                    Properties.Settings.Default.Save();
                    MainPage.newDocumentFound = true;
                }
                else
                {
                    try
                    {
                        if (System.IO.Path.GetExtension(e.FullPath.ToUpper()) != ".CRDOWNLOAD")
                        {
                            File.Delete(e.FullPath);
                        }
                    } catch { }
                }
            }
        }

        private void StartFolderWatcher(string folder)
        {
            // Temporary Watcher code
            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = folder;

            // Watch for all changes specified in the NotifyFilters
            //enumeration.
            watcher.NotifyFilter = NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.DirectoryName |
            NotifyFilters.FileName |
            NotifyFilters.LastAccess |
            NotifyFilters.LastWrite |
            NotifyFilters.Security |
            NotifyFilters.Size;

            // Watch all files.
            watcher.Filter = "";

            // Add event handlers.
            watcher.Created += new FileSystemEventHandler(watcher_OnChanged);

            //Start monitoring.
            watcher.EnableRaisingEvents = true;
        }

        private void mainPage_Loaded(object sender, RoutedEventArgs e)
        {
            animation.From = Colors.Orange;
            animation.To = Colors.Gray;
            animation.Duration = new Duration(TimeSpan.FromSeconds(1));
            animation.RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;
            this.btnRefresh.Background = new SolidColorBrush(Colors.White);
            this.btnInBox.Background = new SolidColorBrush(Colors.White);
            this.btnOutBox.Background = new SolidColorBrush(Colors.White);
            //this.btnRefresh.Background.BeginAnimation(SolidColorBrush.ColorProperty, animation);


            // Save the document grid location and margin info
            Properties.Settings.Default.GridLeft = (int)dgMasterDocs.Margin.Left;
            Properties.Settings.Default.GridTop = (int)dgMasterDocs.Margin.Top;
            Properties.Settings.Default.GridHeight = (int)dgMasterDocs.Height;
            Properties.Settings.Default.GridMargin = dgMasterDocs.Margin.ToString();

            // Set the status of "Show Only ISO Documents" checkbox.
            chkShowISODocumentsOnly.IsChecked = Properties.Settings.Default.ShowISODocumentsOnly;

            // Set the status of "Continuous Scanning".
            chkContinuousScanning.IsChecked = Properties.Settings.Default.ContinuousScanning;

            // Set the color of the prompts at the bottom of the main screen.
            lblGreen.Background = System.Windows.Media.Brushes.Green;
            lblRed.Background = System.Windows.Media.Brushes.Red;
            lblYellow.Background = System.Windows.Media.Brushes.Yellow;

            dgMasterDocs.SelectionMode = (DataGridSelectionMode)SelectionMode.Single;

            MainPageTimer.Tick += new EventHandler(MainPageTimer_Tick);
            MainPageTimer.Interval = TimeSpan.FromMilliseconds(250);
            MainPageTimer.Start();

            PageReloadTimer.Tick += new EventHandler(PageReloadTimer_Tick);
            PageReloadTimer.Interval = TimeSpan.FromMilliseconds(2000);
            PageReloadTimer.Start();
            //if (buck.GetCurrentUserName() == "earl.francis") { PageReloadTimer.Start(); }

            Properties.Settings.Default.documentSearchInProgress = false;
            Properties.Settings.Default.Save();

            System.IO.Directory.CreateDirectory(@"C:\Temp");

            SetUserProgramRights();

            currentUser = Properties.Settings.Default.CurrentUsername;

            // Create a user folder for the program to monitor
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\ISO-DMS\" +
                Properties.Settings.Default.CurrentUsername.ToString();
            if (!Directory.Exists(userFolder)) { Directory.CreateDirectory(userFolder); }

            StartFolderWatcher(userFolder);

            if (!editingDocument)
            { tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0, "", false, Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly); }
            else
            { editingDocument = false; }

        }

        private void dgMasterDocs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentMasterDocsGridRow = tools.GetGridRow(dgMasterDocs);
        }

        private void ViewPrintDocument()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {

                int cellValue = 0;
                foreach (DataRowView row in dgMasterDocs.SelectedItems)
                {
                    cellValue = (int)row.Row.ItemArray[0];
                }

                string inDocument = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ControlledFileLink"].ToString();
                string outDocument = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["UncontrolledFileLink"].ToString();
                tools.ViewPrintDocument(dsMasterDocs, cellValue, inDocument, outDocument);
            }
        }

        private void btnPrintUncontrolled_Click(object sender, RoutedEventArgs e)
        {
            ViewPrintDocument();
        }

        private void SystemSetup()
        {
            NavigationService.Navigate(new SystemSetup());
        }

        private void btnSetup_Click(object sender, RoutedEventArgs e)
        {
            SystemSetup();
        }

        private void btnEditProperties_Click(object sender, RoutedEventArgs e)
        {
            EditDocumentProperties();
        }

        private void EditDocumentProperties()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {

                // Store the current datagrid row.
                int oldRow = currentMasterDocsGridRow;

                int cellValue = (int)dsMasterDocs.Tables[0].Rows[oldRow]["ID"];

                DataSet ds = new DataSet();
                string strSQL = "SELECT DocumentMaster.*, Departments.DepartmentName FROM DocumentMaster " +
                    "LEFT OUTER JOIN Departments ON DocumentMaster.DepartmentID = Departments.ID WHERE DocumentMaster.ID = " + cellValue;


                tools.DBOpenSQLDB();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = strSQL;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);

                buck.DBCloseDatabase();


                // Make sure the user has the security clearance to edit this document.
                string documentCreator = ds.Tables[0].Rows[0]["CreatedBy"].ToString();
                string currentUser = Properties.Settings.Default.CurrentUsername;
                int currentUserSecurityLevel = Properties.Settings.Default.CurrentUserSecurityLevel;

                // Store the Department ID
                int departmentID = (int)ds.Tables[0].Rows[0]["DepartmentID"];

                bool canEdit = false;

                if (documentCreator != currentUser)
                {
                    // The current user is NOT the document creator.
                    switch (currentUserSecurityLevel)
                    {
                        case SecurityLevel.SystemAdmin:
                            if (tools.IsUserADepartmentAdmin(currentUser, departmentID) || currentUserSecurityLevel == SecurityLevel.SystemAdmin)
                            {
                                canEdit = true;
                            }
                            break;
                    }
                }
                else
                {
                    canEdit = true;
                }

                // The current user did not pass any of the security tests.
                if (!canEdit)
                {
                    MessageBox.Show("You are not allowed to edit a document that was created by another user.", "Notice");
                    return;
                }

                EditDocumentProperties docEdit = new EditDocumentProperties();

                // Send a flag to the form as to whether or not this is the document creator.
                docEdit.IsDocumentCreator = (documentCreator == currentUser);

                docEdit.documentID = (int)ds.Tables[0].Rows[0]["ID"];
                docEdit.isoType = ds.Tables[0].Rows[0]["ISOType"].ToString();
                docEdit.isoTier = ds.Tables[0].Rows[0]["ISOTier"].ToString();
                docEdit.isoTag = ds.Tables[0].Rows[0]["ISOTag"].ToString();
                docEdit.isoRevision = ds.Tables[0].Rows[0]["ISORevision"].ToString();
                docEdit.isoDocumentName = ds.Tables[0].Rows[0]["Title"].ToString();
                docEdit.isoDocumentTitle = ds.Tables[0].Rows[0]["Title"].ToString();
                docEdit.isoDepartment = ds.Tables[0].Rows[0]["DepartmentName"].ToString();
                docEdit.chkMakePrivate.IsChecked = (bool)ds.Tables[0].Rows[0]["IsPrivate"];
                docEdit.chkMakePublic.IsChecked = (bool)ds.Tables[0].Rows[0]["IsPublic"];
                docEdit.dtpTransactionDate.Text = ds.Tables[0].Rows[0]["TransactionDate"].ToString();
                docEdit.txtTransactionAmount.Text = ds.Tables[0].Rows[0]["TransactionAmount"].ToString();
                docEdit.ShowDialog();

                if (!Properties.Settings.Default.documentSearchInProgress)
                {
                    tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, oldRow);
                    currentMasterDocsGridRow = oldRow;
                }
                else
                {
                    tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, currentMasterDocsGridRow, Properties.Settings.Default.documentSearchSQLText);
                }
            }

        }

        private void EmailUncontrolledDocument()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {

                int cellValue = 0;
                foreach (DataRowView row in dgMasterDocs.SelectedItems)
                {
                    cellValue = (int)row.Row.ItemArray[0];
                }

                DataSet ds = new DataSet();
                string strSQL = "SELECT * FROM DocumentMaster WHERE ID = " + cellValue;

                tools.DBOpenSQLDB();

                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = strSQL;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);

                tools.DBCloseSQLDatabase();

                int fileID = (int)ds.Tables[0].Rows[0]["ID"];
                string fileTitle = ds.Tables[0].Rows[0]["Title"].ToString();
                string fileURL = ds.Tables[0].Rows[0]["UncontrolledFileLink"].ToString();

                if (File.Exists(fileURL))
                {
                    SendEmail SendEmail = new SendEmail(fileTitle, fileURL, fileID);
                    SendEmail.ShowDialog();
                }
                else
                {
                    MessageBox.Show("The requested file cannot be found.  It may have been deleted by an administrator.", "Notice", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
        }

        private void btnEmailUncontrolled_Click(object sender, RoutedEventArgs e)
        {
            EmailUncontrolledDocument();
        }

        private void SearchDocuments()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {
                SearchDocuments SearchDocuments = new SearchDocuments();
                SearchDocuments.ShowDialog();

                if (SearchDocuments.SQLText.Length > 0)
                {
                    tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0, SearchDocuments.SQLText, true, Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly);
                }
                else
                {
                    tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, currentMasterDocsGridRow, "", Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly);
                }
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchDocuments();
        }

        private void RefreshDocuments()
        {
            dgMasterDocs.Items.SortDescriptions.Clear();
            dgMasterDocs.Items.Refresh();

            foreach (DataGridColumn column in dgMasterDocs.Columns)
            {
                column.SortDirection = null;
            }

            Properties.Settings.Default.documentSearchInProgress = false;
            Properties.Settings.Default.cancelRefreshMasterDocuments = true;
            Properties.Settings.Default.Save();
            tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0, "", true, Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly);

        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshDocuments();
        }

        public static void SaveClipboardImageToFile(string filePath)
        {
            var image = Clipboard.GetImage();
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }
        }

        private void DeleteDocument()
        {
            string docControlledFileName = "";
            string docUncontrolledFileName = "";
            string ControlledShortFileName = "";
            string UncontrolledShortFileName = "";

            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {
                // Get the ID of the current document.
                //int id = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];

                int oldRow = currentMasterDocsGridRow;

                int docID = 0;
                string docTitle = "";
                foreach (DataRowView row in dgMasterDocs.SelectedItems)
                {
                    docID = (int)row.Row.ItemArray[0];
                    docTitle = row.Row.ItemArray[5].ToString();
                    docControlledFileName = row.Row.ItemArray[9].ToString();
                    docUncontrolledFileName = row.Row.ItemArray[10].ToString();

                    ControlledShortFileName = System.IO.Path.GetFileName(docControlledFileName);
                    UncontrolledShortFileName = System.IO.Path.GetFileName(docUncontrolledFileName);

                    // Make sure the user has the security clearance to edit this document.
                    string documentCreator = row.Row.ItemArray[13].ToString();
                    string currentUser = Properties.Settings.Default.CurrentUsername;
                    int currentUserSecurityLevel = Properties.Settings.Default.CurrentUserSecurityLevel;

                    int departmentID = (int)row.Row.ItemArray[19] ;

                    bool isDeprecated = (bool)row.Row.ItemArray[16];

                    if (buck.GetCurrentUserName() != "efrancis")
                    {
                        if (isDeprecated)
                        {
                            MessageBox.Show("You cannot delete a document with that has a status of Revision Deprecated.", "Notice");
                            return;
                        }
                    }

                    bool canEdit = false;
                    if (documentCreator != currentUser)
                    {
                        switch (currentUserSecurityLevel)
                        {
                            case SecurityLevel.SystemAdmin:
                                if (tools.IsUserADepartmentAdmin(currentUser, departmentID) || currentUserSecurityLevel == SecurityLevel.SystemAdmin)
                                {
                                    canEdit = true;
                                }
                                break;
                        }
                    }
                    else
                    {
                        canEdit = true;
                    }

                    if (!canEdit)
                    {
                        MessageBox.Show("You are not allowed to delete a document that was created by another user.", "Notice");
                        return;
                    }

                }

                // Search link tables for the document's existence.  If found, don't delete.
                tools.DBOpenSQLDB();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = tools.cnSQLDB;
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = "SELECT COUNT(*) FROM DocumentLink WHERE Document_ID = " + docID;
                int returnValue1 = (int)cmd.ExecuteScalar();

                cmd.CommandText = "SELECT COUNT(*) FROM SOPJobCodeLinks WHERE Document_ID = " + docID;
                int returnValue2 = (int)cmd.ExecuteScalar();

                if (returnValue1 > 0 || returnValue2 > 0)
                {
                    // Cannot delete the document at this time.
                    MessageBox.Show("This document cannot be deleted as it is currently linked to one or more Job Codes.", "Notice");
                }
                else
                {
                    // Delete the document if the user confirms.
                    //string docTitle = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString();
                    //int docID = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];
                    if (MessageBox.Show("Delete the document:\n [" + docTitle + "] ?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        if (MessageBox.Show("Are you sure?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                        {
                            cmd.CommandText = "DELETE FROM DocumentMaster WHERE ID = " + docID;
                            cmd.ExecuteNonQuery();
                            tools.WriteSecurityLogEntry(docID, tools.logEvent_DeletedDocument, docTitle);

                            // Move the Controlled and Uncontrolled versions to the Deleted folder.
                            if (File.Exists(DeletedDocPath + ControlledShortFileName)) File.Delete(DeletedDocPath + ControlledShortFileName);
                            if (File.Exists(DeletedDocPath + UncontrolledShortFileName)) File.Delete(DeletedDocPath + UncontrolledShortFileName);

                            if (File.Exists(docControlledFileName)) File.Move(docControlledFileName, DeletedDocPath + ControlledShortFileName);
                            if (File.Exists(docUncontrolledFileName)) File.Move(docUncontrolledFileName, DeletedDocPath + UncontrolledShortFileName);

                            if (currentMasterDocsGridRow > dsMasterDocs.Tables[0].Rows.Count - 1)
                            {
                                currentMasterDocsGridRow -= 1;
                            }

                            if (!Properties.Settings.Default.documentSearchInProgress)
                            {
                                tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, currentMasterDocsGridRow);
                            }
                            else
                            {
                                tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, currentMasterDocsGridRow, Properties.Settings.Default.documentSearchSQLText);
                            }

                            Properties.Settings.Default.documentCountUpdatedByCurrentUser = true;
                            Properties.Settings.Default.Save();
                        }
                    }
                }
            }

        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void btnUpdateRevision_Click(object sender, RoutedEventArgs e)
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {
                // Get the ID of the current document.
                //int id = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];

                int oldRow = currentMasterDocsGridRow;

                int docID = 0;
                string docTitle = "";
                string docType = "";
                string docTier = "";
                string docTag = "";
                int docDepartmentID = 0;
                bool docIsPrivate = false;
                bool docIsPublic = false;
                bool canRevise = false;

                foreach (DataRowView row in dgMasterDocs.SelectedItems)
                {
                    docID = (int)row.Row.ItemArray[0];
                    docTitle = row.Row.ItemArray[5].ToString();
                    docType = row.Row.ItemArray[1].ToString();
                    docTier = row.Row.ItemArray[2].ToString();
                    docTag = row.Row.ItemArray[3].ToString();
                    docDepartmentID = (int)row.Row.ItemArray[20];
                    docIsPrivate = (bool)row.Row.ItemArray[16];
                    docIsPublic = (bool)row.Row.ItemArray[17];

                    // Make sure the user has the security clearance to edit this document.
                    string documentCreator = row.Row.ItemArray[11].ToString();
                    string currentUser = Properties.Settings.Default.CurrentUsername;
                    int currentUserSecurityLevel = Properties.Settings.Default.CurrentUserSecurityLevel;

                    int departmentID = (int)row.Row.ItemArray[20];

                    if (documentCreator != currentUser)
                    {
                        switch (currentUserSecurityLevel)
                        {
                            case SecurityLevel.SystemAdmin:
                                canRevise = true;
                                break;
                        }
                    }
                    else
                    {
                        if (tools.IsUserADepartmentAdmin(currentUser, departmentID) || currentUserSecurityLevel == SecurityLevel.SystemAdmin)
                        {
                            canRevise = true;
                        }
                        break;
                    }

                }

                if (!canRevise)
                {
                    MessageBox.Show("You are not allowed to check in a revision for a document that was created by another user.", "Notice");
                    return;
                }
                else
                {
                    if (MessageBox.Show("Check in a revised version of:\n" + docTitle + "?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
                    {
                        return;
                    }
                    else
                    {
                        Properties.Settings.Default.RevisonCheckedIn = true;
                        Properties.Settings.Default.Save();
                    }
                }

                // Security checks have been passed, so start the revision check-in process.
                CheckInFilePicker();


                if (Properties.Settings.Default.RevisonCheckedIn)
                {
                    // Get the ID for the newly imported document revision
                    tools.DBOpenSQLDB();

                    string controlledFileLink = @Properties.Settings.Default.RevistedDocumentPath;

                    //MessageBox.Show(Properties.Settings.Default.RevistedDocumentPath.ToString());

                    SqlCommand cmd = new SqlCommand();
                    cmd.Connection = tools.cnSQLDB;
                    cmd.CommandType = CommandType.Text;
                    //cmd.CommandText = "SELECT MAX(ID) FROM DocumentMaster";
                    cmd.CommandText = "SELECT ID FROM DocumentMaster WHERE ControlledFileLink = '" + controlledFileLink + "'";

                    int newDocID = (int)cmd.ExecuteScalar();

                    // Update properties for the new document.
                    cmd.CommandText = "UPDATE DocumentMaster SET Title = " + chr39 + docTitle + chr39 + ", ISOType = " + chr39 + docType + chr39 + ", "
                        + "ISOTier = " + chr39 + docTier + chr39 + ", ISOTag = " + chr39 + docTag + chr39 + ", "
                        + "IsPrivate = " + Convert.ToInt32(docIsPrivate) + ", IsPublic = " + Convert.ToInt32(docIsPublic) + ", "
                        + "DepartmentID = " + docDepartmentID + " "
                        + "WHERE ID = " + newDocID;

                    cmd.ExecuteNonQuery();

                    // Flag the old document as deprecated.
                    cmd.CommandText = "UPDATE DocumentMaster SET IsPrivate = 1, IsPublic = 0, IsDeprecated = 1, "
                        + "DateDeprecated = " + chr39 + DateTime.Now.ToString() + chr39 + " "
                        + "WHERE ID = " + docID;
                    cmd.ExecuteNonQuery();

                    // Update the document link tables to reflect the new document ID.
                    cmd.CommandText = "UPDATE DocumentLink SET Document_ID = " + newDocID + " WHERE Document_ID = " + docID;
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "UPDATE SOPJobCodeLinks SET Document_ID = " + newDocID + " WHERE Document_ID = " + docID;
                    cmd.ExecuteNonQuery();

                    tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0);
                    tools.WriteSecurityLogEntry(docID, "Checked In New Revision", docTitle);

                    buck.DBCloseDatabase();

                    //EditDocumentProperties();
                }

                // Reset the checked in flag.
                Properties.Settings.Default.RevisonCheckedIn = true;
                Properties.Settings.Default.Save();
            }
        }

        private void dgMasterDocs_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Handles to custom coloring of the first cell in the grid.
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new System.Action(() => AlterRow(dgMasterDocs, e)));
        }

        private void AlterRow(DataGrid dg, DataGridRowEventArgs e)
        {

            try
            {
                var cell = GetCell(dg, e.Row, 0);
                if (cell == null)
                {
                    return;
                }
                else
                {
                    cell.Background = System.Windows.Media.Brushes.White;
                    cell.Foreground = System.Windows.Media.Brushes.Black;
                }

                DataRowView item = e.Row.Item as DataRowView;
                if (item != null)
                {

                    DataRow row = item.Row;
                    if ((bool)row["IsPublic"] == true)
                    {
                        cell.Background = System.Windows.Media.Brushes.Green;
                        cell.Foreground = System.Windows.Media.Brushes.White;
                    }

                    if ((bool)row["IsPrivate"] == true)
                    {
                        cell.Background = System.Windows.Media.Brushes.Red;
                        cell.Foreground = System.Windows.Media.Brushes.White;
                    }

                    if ((bool)row["IsDeprecated"] == true)
                    {
                        cell.Background = System.Windows.Media.Brushes.Yellow;
                        cell.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }
            }
            catch
            {
            }
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

        private DataGridRow GetRow(DataGrid grid, int index)
        {
            var row = grid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;

            if (row == null)
            {
                // May be virtualized, bring into view and try again.
                grid.ScrollIntoView(grid.Items[index]);
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        private void chkHideRevisedVersions_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HideRevisions = (bool)this.chkHideRevisedVersions.IsChecked;
            Properties.Settings.Default.Save();

            tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0, "", true, Properties.Settings.Default.HideRevisions, Properties.Settings.Default.ShowISODocumentsOnly);
        }

        private void EditDocument()
        {
            // Only a system administrator can utilize this functionality.
            if (tools.GetUserSecurityLevel(buck.GetCurrentUserName()) == SecurityLevel.SystemAdmin)
            {
                string fileName = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ControlledFileLink"].ToString();
                string docTitle = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString();
                string docType = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["DocumentType"].ToString();
                string user = buck.GetCurrentUserName();
                int docID = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];

                tools.WriteSecurityLogEntry(docID, tools.logEvent_ModifiedDocument, docTitle);

                int x = currentMasterDocsGridRow;

                // Only allow Word, Excel or PowerPoint documents to be edited.
                List<string> DocTypes = new List<string> { "WORD", "EXCEL", "POWERPOINT" };
                if (DocTypes.Contains(docType))
                {
                    if (MessageBox.Show("Edit document:\n" + docTitle + "?", "Notice", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(fileName);

                    }
                }
            }
        }

        private void dgMasterDocs_KeyDown(object sender, KeyEventArgs e)
        {
            editingDocument = true;

            // Show the login screen.
            if (Keyboard.IsKeyDown(Key.F8))
            {
                Login login = new Login();
                login.Left = (1366 / 2) - (login.Width / 2);
                login.Top = (768 / 2) - (login.Height / 2);
                login.ShowDialog();

                if (login.txtUsername.Text.Length > 0)
                {
                    SetUserProgramRights(login.txtUsername.Text);
                    tools.CheckUserCredentials(login.txtUsername.Text);
                }
            }

            // Only a system administrator can utilize this functionality.
            if (tools.GetUserSecurityLevel(buck.GetCurrentUserName()) == SecurityLevel.SystemAdmin)
            {

                if (Keyboard.IsKeyDown(Key.F12))
                {
                    MsgBox msg = new MsgBox(@"Exporting selected document to C:\Temp");
                    msg.Show();
                    System.Threading.Thread.Sleep(1000);

                    string inFileName = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["UncontrolledFileLink"].ToString();
                    string outFileName = tools.RemoveReservedFileSystemCharacters(dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString() + ".pdf");

                    File.Copy(inFileName, @"C:\Temp\" + outFileName, true);
                    msg.Close();
                }

                if (Keyboard.IsKeyDown(Key.F5))
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    {
                        string controlledDocPath = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ControlledFileLink"].ToString();
                        string uncontrolledDocPath = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["UncontrolledFileLink"].ToString();
                        string docTitle = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString();
                        string docType = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["DocumentType"].ToString();
                        string user = buck.GetCurrentUserName();
                        int docID = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];

                        tools.WriteSecurityLogEntry(docID, tools.logEvent_ModifiedDocument, docTitle);

                        int x = currentMasterDocsGridRow;

                        // Only allow Word, Excel or PowerPoint documents to be edited.
                        List<string> DocTypes = new List<string> { "WORD", "EXCEL", "POWERPOINT" };
                        if (DocTypes.Contains(docType))
                        {
                            // The default application will be used to open the document.
                            var docViewProcess = new Process { StartInfo = new ProcessStartInfo(controlledDocPath) };
                            docViewProcess.Start();
                        }
                    }
                }
            }
        }

        private void dgMasterDocs_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ScanDocuments()
        {
        }

        private void btnScanIn_Click(object sender, RoutedEventArgs e)
        {
            bool continueScanning = Properties.Settings.Default.ContinuousScanning;

            do
            {
                if (MessageBox.Show("Load documents in scanner and then click OK or CANCEL to exit.", "Scanner Options", MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.Cancel) == MessageBoxResult.OK)
                {
                    //string fileName = tools.UncontrolledDocPath + tools.getNextDMSFileName() + ".pdf";
                    string fileName = @"C:\Temp\" + tools.getNextDMSFileName().Replace(@"\", "") + ".pdf";
                    ScanMinimal(fileName);

                    if (File.Exists(fileName))
                    {
                        if (!Properties.Settings.Default.scanningCancelled)
                        {
                            CheckInScannedPDFDocument(fileName, "PDF");
                            currentMasterDocsGridRow = 0;
                            if (!Properties.Settings.Default.ContinuousScanning)
                            { EditDocumentProperties(); }
                        }
                        else
                        {
                            Properties.Settings.Default.scanningCancelled = false;
                            Properties.Settings.Default.Save();
                            MessageBox.Show("Document scanning was cancelled.", "Notice");
                        }
                    }
                }
                else
                {
                    continueScanning = false;
                }

            } while (continueScanning);

        }

        static IImageCollector ScanMinimal(string fileName)
        {

            bool isValidLicense = GlobalConfig.SetLicenseKey("Buck Company", "g/4JFMjn6KuO9wj8QNXi1FWCXPEAuhZMNskd1F5fpAIU4rE0KKwoH + XQtyGxBiLg");

            Tools tools = new Tools();
            BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();

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
                        if (ds == null)
                        {
                            Console.WriteLine("Unable to open source");
                            return null;
                        }
                        ImageCollector imgCol = new ImageCollector();
                        collector.AddCollector(imgCol);
                        DataSource.ErrorInfo ei = new DataSource.ErrorInfo();

                        MsgBox msg = new MsgBox("Scanning your document(s) ...please stand by.");
                        msg.Show();
                        buck.DoEvents();

                        ds.Acquire(collector, false, true);

                        //MessageBox.Show(ei.ReturnCode.ToString());
                        //MessageBox.Show(ei.Message.ToString());

                        if (ei.ReturnCode != TwRC.Cancel)
                        {
                            imgCol.SaveAllToMultipagePdf(fileName);
                        }
                        else
                        {
                            Properties.Settings.Default.scanningCancelled = true;
                            Properties.Settings.Default.Save();
                        }

                        imgCol.Dispose();

                        msg.Close();

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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            EditDocument();
        }

        private void showHelp()
        {
            Help help = new Help("MainPage");
            help.ShowDialog();
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            Help help = new Help("MainPage");
            help.ShowDialog();
        }

        private void AddDocumentToApplication()
        {
            string documentID = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"].ToString();
            string pageDescription = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString();
            string pageID = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ISOTag"].ToString();
            string sql = "SELECT COUNT(*) FROM HelpTopics WHERE PageID = " + tools.chr39 + pageID + tools.chr39 +
                " AND DocumentID = " + documentID;

            int result = tools.DBExecuteScalar(sql);

            if (result == 0)
            {
                sql = "INSERT INTO HelpTopics (PageID, DocumentID, PageDescription) " +
                    "VALUES(" + tools.chr39 + pageID + tools.chr39 + "," + documentID + ", " +
                    tools.chr39 + pageDescription + tools.chr39 + ")";

                tools.DBExecuteNonQuery(sql);
            }
        }

        private void chkShowISODocumentsOnly_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowISODocumentsOnly = (bool)chkShowISODocumentsOnly.IsChecked;
            Properties.Settings.Default.Save();

            tools.LoadMasterDocs(dsMasterDocs, dgMasterDocs, 0, "", true, (bool)chkHideRevisedVersions.IsChecked, (bool)chkShowISODocumentsOnly.IsChecked);

        }

        private void chkShowISODocumentsOnly_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void mnuItemRefreshUncontrolledDocument_Click(object sender, RoutedEventArgs e)
        {

            MsgBox msg = new MsgBox("Refreshing the selected uncontrolled document ...please stand by.");
            msg.Show();

            string controlledDocPath = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ControlledFileLink"].ToString();
            string uncontrolledDocPath = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["UncontrolledFileLink"].ToString();

            // Refresh the uncontrolled document based on the file extension
            string fileExtension = System.IO.Path.GetExtension(controlledDocPath.ToString());
            switch (fileExtension.ToUpper())
            {
                case ".DOCX":
                    tools.RefreshWordDocument(controlledDocPath, uncontrolledDocPath);
                    break;
                case ".DOC":
                    tools.RefreshWordDocument(controlledDocPath, uncontrolledDocPath);
                    break;
                case ".XLSX":
                    tools.RefreshExcelDocument(controlledDocPath, uncontrolledDocPath);
                    break;
                case ".XLS":
                    tools.RefreshExcelDocument(controlledDocPath, uncontrolledDocPath);
                    break;
                case ".PPTX":
                    tools.RefreshPowerPointDocument(controlledDocPath, uncontrolledDocPath);
                    break;
                case ".PPT":
                    tools.RefreshPowerPointDocument(controlledDocPath, uncontrolledDocPath);
                    break;
            }

            msg.Close();
            MessageBox.Show("The uncontrolled version of this document has been refreshed.", "Notice");
        }

        private void mnuItemViewPrintDocument_Click(object sender, RoutedEventArgs e)
        {
            if (chkPrintAll.IsChecked == false )
            {
                ViewPrintDocument();
            }
            else
            {
                chkPrintAll.IsChecked = false;
                MsgBox msg = new MsgBox("Printing the requested document(s) ...please stand by.");
                msg.Show();
                buck.DoEvents();

                for (int x = 0; x <= dsMasterDocs.Tables[0].Rows.Count - 1; x++)
                {
                    string fileName = dsMasterDocs.Tables[0].Rows[x]["UncontrolledFileLink"].ToString();
                    tools.SendToPrinter(fileName);
                }

                msg.Close();
            }
        }

        private void mnuItemEmailDocument_Click(object sender, RoutedEventArgs e)
        {
            EmailUncontrolledDocument();
        }

        private void mnuItemEditDocumentProperties_Click(object sender, RoutedEventArgs e)
        {
            EditDocumentProperties();
        }

        private void mnuItemSearchDocuments_Click(object sender, RoutedEventArgs e)
        {
            SearchDocuments();
        }

        private void mnuItemRefreshDocuments_Click(object sender, RoutedEventArgs e)
        {
            RefreshDocuments();
        }

        private void mnuItemDeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            DeleteDocument();
        }

        private void mnuItemSystemSetup_Click(object sender, RoutedEventArgs e)
        {
            SystemSetup();
        }

        private void mnuItemHelp_Click(object sender, RoutedEventArgs e)
        {
            showHelp();
        }

        private void dgMasterDocs_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ViewPrintDocument();
        }

        private void chkContinuousScanning_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ContinuousScanning = (bool)chkContinuousScanning.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void btnCaptureFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                SaveClipboardImageToFile(@"C:\Temp\imageTemp.jpg");
                Clipboard.Clear();
                CheckInImageDocument(@"C:\Temp\imageTemp.jpg", @"C:\Temp\imageTemp.jpg", "Image");
                ViewPrintDocument();
                EditDocumentProperties();
            }
            else
            {
                MessageBox.Show("The clipboard is empty.  There is nothing to save!", "Notice");
            }
        }

        private void dgMasterDocs_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTime))
            {
                if (e.Column.Header.ToString() == "TransactionDate" || e.Column.Header.ToString() == "EffectiveDate")
                {
                    ((DataGridTextColumn)e.Column).Binding.StringFormat = "MM/dd/yyyy";
                }
                else
                {
                    ((DataGridTextColumn)e.Column).Binding.StringFormat = "MM/dd/yyyy hh:mm:ss tt";
                }
            }

            if (e.PropertyType == typeof(decimal))
            {
                ((DataGridTextColumn)e.Column).Binding.StringFormat = "$0.00";
                Align(e.Column, TextAlignment.Right);
            }
        }

        private void Align(DataGridColumn c, TextAlignment a)
        {
            System.Windows.Style s = new System.Windows.Style();
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, a));
            c.CellStyle = s;
        }

        private void btnDepartmentModules_Click(object sender, RoutedEventArgs e)
        {
            ShowDepartmentModulesMenu();
        }

        private void ShowDepartmentModulesMenu()
        {
            NavigationService.Navigate(new DepartmentModulesMenu());
        }

        private void btnNotes_Click(object sender, RoutedEventArgs e)
        {
            ShowDocumentNotes();
        }

        private void ShowDocumentNotes()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {
                int docID = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];
                string docTitle = dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["Title"].ToString();
                DocumentNotes docNotes = new DocumentNotes(docID, docTitle);
                docNotes.ShowDialog();
            }
        }

        private void btnInBox_Click(object sender, RoutedEventArgs e)
        {
            ShowInBox();
        }

        private void ShowInBox()
        {
            string recipient = Properties.Settings.Default.CurrentUsername;
            string sql = "SELECT COUNT(*) FROM Inbox WHERE Recipient = '" + recipient + "'";

            int result = tools.DBExecuteScalar(sql);

            if (result > 0)
            {
                NotifyInBox InBox = new NotifyInBox();
                InBox.ShowDialog();
            }
            else
            {
                MessageBox.Show("Your Inbox is currently empty!", "Notice");
            }
        }

        private void ShowOutBox()
        {
            string sender = Properties.Settings.Default.CurrentUsername;
            string sql = "SELECT COUNT(*) FROM OutboxQueuedDocuments WHERE Sender = '" + sender + "'";

            int result = tools.DBExecuteScalar(sql);

            if (result > 0)
            {
                NotifyOutBox OutBox = new NotifyOutBox();
                OutBox.ShowDialog();
            }
            else
            {
                MessageBox.Show("Your Outbox is currently empty!", "Notice");
            }
        }

        private void btnOutBox_Click(object sender, RoutedEventArgs e)
        {
            ShowOutBox();
        }

        private void AddDocumentToOutboxQueue()
        {
            if (dsMasterDocs.Tables[0].Rows.Count > 0)
            {
                string sender = Properties.Settings.Default.CurrentUsername;
                int documentID = (int)dsMasterDocs.Tables[0].Rows[currentMasterDocsGridRow]["ID"];
                string sql = "SELECT COUNT(*) FROM OutboxQueuedDocuments WHERE DocumentID = " + documentID.ToString();

                int results = tools.DBExecuteScalar(sql);

                if (results == 0)
                {

                    sql = "INSERT INTO OutboxQueuedDocuments (DocumentID, Sender) " +
                        "VALUES(" + documentID.ToString() + ",'" + sender + "')";

                    tools.DBExecuteNonQuery(sql);

                    //MessageBox.Show("Your document has been added to the Outbox Queue!", "");
                }
            }
        }

        private void mnuItemSendToEmployeeInbox_Click(object sender, RoutedEventArgs e)
        {
            AddDocumentToOutboxQueue();
        }
    }
}

