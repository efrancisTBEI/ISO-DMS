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
    /// Interaction logic for InputBoxMultiLine.xaml
    /// </summary>
    public partial class InputBoxMultiLine : Window
    {
        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();
        Tools tools = new Tools();

        public string itemText = "";
        public string cancelButtonText = "";
        public bool deleteItem = false;
        private bool blnSQL = false;

        public InputBoxMultiLine(string title, string item = "", double posLeft = 0, double posTop = 0, bool readOnly = false, bool deleteRecord = false,bool oneLineOnly = false)
        {
            InitializeComponent();

            if (posLeft != 0 || posTop != 0)
            {
                Left = posLeft;
                Top = posTop - Height;
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            txtBlkTitle.Text = title;
            txtItem.Text = item;
            txtItem.SelectionStart = item.Length;

            if (readOnly)
            {
                this.btnCancel.Visibility = Visibility.Hidden;
                txtItem.IsReadOnly = true;
            }

            if (deleteRecord)
            {
                this.btnCancel.Visibility = Visibility.Visible;
                deleteItem = true;
            }

            blnSQL = oneLineOnly;
        }

        private void winInputBoxML_Loaded(object sender, RoutedEventArgs e)
        {
            if (cancelButtonText.Length > 0)
            {
                btnCancel.Content = cancelButtonText;
            }

            if (blnSQL)
            {
                txtItem.MaxLines = 1;
                txtItem.TextWrapping = TextWrapping.NoWrap;
                txtItem.AcceptsReturn = false;
                txtItem.Height = double.NaN;
                this.Height -= 140;
                buck.DoEvents();
            }

            txtItem.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            deleteItem = false;
            Close();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (!deleteItem)
            {
                itemText = txtItem.Text;
            }
            Close();
        }

        private void winInputBoxML_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void txtItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (!deleteItem)
                {
                    itemText = txtItem.Text;
                }
                Close();
            }
        }
    }
}
