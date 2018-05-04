using System;
using System.Collections.Generic;
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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.txtUsername.Text = "";
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtUsername.Focus();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void txtUsername_GotFocus(object sender, RoutedEventArgs e)
        {
            txtUsername.Background = Brushes.Yellow;
        }

        private void txtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            txtUsername.Background = Brushes.White;
        }

        private void txtPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPassword.Background = Brushes.White;
        }

        private void txtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPassword.Background = Brushes.Yellow;
        }
    }
}
