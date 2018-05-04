using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
    /// Interaction logic for SendEmail.xaml
    /// </summary>
    public partial class SendEmail : Window
    {

        Tools tools = new Tools();
        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        string fileAttachment = "";
        int file_ID = 0;

        public SendEmail()
        {
            InitializeComponent();
        }

        public SendEmail(string fileTitle, string fileURL, int fileID)
        {
            InitializeComponent();
            this.tBlkFileName.Text = fileTitle;
            fileAttachment = fileURL;
            file_ID = fileID;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.txtEmailAddress.Focus();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            // First make sure the email address textbox is not empty and that it contains an '@' sign.
            if (this.txtEmailAddress.Text.Length > 0 && this.txtEmailAddress.Text.Contains("@"))
            {
                this.Visibility = Visibility.Hidden;

                MsgBox ShowMsg = new MsgBox("Creating and emailing the requested ISO uncontrolled document ...please stand by");
                ShowMsg.Show();
                buck.DoEvents();

                // Create a copy of the uncontrolled document with a watermark if this is anything other than a PDF.
                string uncontrolledDocument = "";

                if (System.IO.Path.GetExtension(fileAttachment).ToString() != ".pdf")
                {
                    uncontrolledDocument = tools.AddWatermarkToPDF(fileAttachment, file_ID, tBlkFileName.Text);
                }
                else
                {
                    uncontrolledDocument = fileAttachment;
                }

                // Create the email.
                MailMessage msg = new MailMessage();
                Attachment _file = new Attachment(uncontrolledDocument);
                msg.From = new MailAddress("buckcompany@buckcompany.com", "Buck Company ISO Administrator");

                // Validate the email address entered by the user.
                try
                {
                    msg.To.Add(new MailAddress(this.txtEmailAddress.Text));
                }
                catch
                {
                    ShowMsg.Close();
                    MessageBox.Show("The email address is empty or invalid.  Please try again.", "Notice!");
                    this.Visibility = Visibility.Visible;
                    this.txtEmailAddress.Focus();
                    return;
                }

                msg.Subject = "Buck Company Uncontrolled ISO Document Attached";
                msg.Body = "The Buck Company ISO Administrator has sent you the attached uncontrolled document\n\n";
                msg.Body += System.IO.Path.ChangeExtension(this.tBlkFileName.Text, ".pdf");
                msg.Attachments.Add(_file);

                // Send the email
                SmtpClient smtpServer = new SmtpClient();
                smtpServer.Credentials = new NetworkCredential("buckcompany@buckcompany.com", "hzdbeyz998kr");
                smtpServer.Port = 587;
                smtpServer.Host = "smtp.gmail.com";
                smtpServer.EnableSsl = true;
                smtpServer.Send(msg);
                buck.DoEvents();

                // Write the security log entry for this email.
                tools.WriteSecurityLogEntry(file_ID, "Emailed Document to: " + txtEmailAddress.Text, tBlkFileName.Text);
                ShowMsg.Close();
                this.Close();
            }
            else
            {
                MessageBox.Show("The email address is empty or invalid.  Please try again.", "Notice!");
                this.txtEmailAddress.Focus();
            }
        }
    }
}
