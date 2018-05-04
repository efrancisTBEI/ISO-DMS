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
    /// Interaction logic for MsgBox.xaml
    /// </summary>
    public partial class MsgBox : Window
    {

        BuckUtils.DotNetUtils buck = new BuckUtils.DotNetUtils();

        public MsgBox()
        {
            InitializeComponent();
        }

        public MsgBox(string msg)
        {
            InitializeComponent();
            label.Content = msg;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            label.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.SizeToContent = SizeToContent.WidthAndHeight;
        }
    }
}
