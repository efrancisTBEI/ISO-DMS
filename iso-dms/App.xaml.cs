using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;

namespace ISO_DMS
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        public string CurrentUsername;

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // Inform the user that all hope is lost.
            string errorMsg = "A program error in the ISO-DMS system has occured.\nThe error text has been emailed to the IT Department.\n\n";
            errorMsg += "This program will now close. Please restart to continue.";
            MessageBox.Show(errorMsg, "Input Required", MessageBoxButton.OK, MessageBoxImage.Error);

            // Compose the email to IT.
            MailMessage msg = new MailMessage();
            msg.To.Add("earl.francis@buckcompany.com");
            msg.From = new MailAddress("buckcompany@buckcompany.com", "Buck Company ISO-DMS System Administrator");
            msg.Subject = "A Program Error in the ISO-DMS System Has Occurred.";
            msg.Body += "The user: [" + buck.GetCurrentUserName().ToUpper() + "] has experienced a program Error In the ISO-DMS System.\n\n";
            msg.Body += e.Exception.ToString();

            // Send the email.
            SmtpClient s = new SmtpClient();
            s.Credentials = new NetworkCredential("buckcompany@buckcompany.com", "hzdbeyz998kr");
            s.Port = 587;
            s.Host = "smtp.gmail.com";
            s.EnableSsl = true;
            s.Send(msg);

            // Turn out the lights and lock the doors.
            e.Handled = false;
            System.Environment.Exit(0);
        }

    }
}
