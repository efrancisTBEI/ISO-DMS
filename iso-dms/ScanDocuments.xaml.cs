using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwainScanning;
using TwainScanning.Collectors;
using TwainScanning.NativeStructs;
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
    /// Interaction logic for ScanDocuments.xaml
    /// </summary>
    public partial class ScanDocuments : Window
    {

        public ScanDocuments()
        {
            InitializeComponent();
        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {
            ScanMinimal();
        }

        static IImageCollector ScanMinimal()
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

                        string fileName = tools.UncontrolledDocPath + tools.getNextDMSFileName() + ".pdf";

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

    }
}
