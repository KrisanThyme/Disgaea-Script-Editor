using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf;

namespace DisgaeaScriptEditor
{
    public partial class MainWindow : Window 
    {
        public static string UserFile;
        public static string UserFolder; 
        public static byte[] WorkingFile;

        static VistaOpenFileDialog ofd = new VistaOpenFileDialog();
        static VistaFolderBrowserDialog fbd = new VistaFolderBrowserDialog();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void UnpackButton_Click(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "DAT File(*.dat)|*.dat";
            ofd.ShowDialog();
            UserFile = ofd.FileName;

            if (!string.IsNullOrEmpty(UserFile))
            {
                Formats.DAT.Unpack();
                System.Windows.Forms.MessageBox.Show("Unpacking Complete.", "Script Unpacker");
            }
            else
            {
                return;
            }
        }

        private void PackButton_Click(object sender, RoutedEventArgs e)
        {
            fbd.ShowDialog();
            UserFolder = fbd.SelectedPath;

            if (!string.IsNullOrEmpty(UserFolder))
            {
                Formats.DAT.Pack();
                System.Windows.Forms.MessageBox.Show("Packing Complete.", "Script Packer");
            }
            else
            {
                return;
            }
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "BIN File(*.bin)|*.bin";
            ofd.ShowDialog();
            UserFile = ofd.FileName;

            if (!string.IsNullOrEmpty(UserFile))
            {
                WorkingFile = File.ReadAllBytes(UserFile);
                Formats.BIN.Parse();
                System.Windows.Forms.MessageBox.Show("Parsing Complete.", "Script Parser");
            }
            else
            {
                return;
            }
        }

        private void ParseAllButton_Click(object sender, RoutedEventArgs e)
        {
            fbd.ShowDialog();
            UserFolder = fbd.SelectedPath;

            if (!string.IsNullOrEmpty(UserFolder))
            {
                foreach (string scrfile in Directory.EnumerateFiles(UserFolder, "*.bin"))
                {
                    UserFile = scrfile;
                    WorkingFile = File.ReadAllBytes(UserFile);
                    Formats.BIN.Parse();
                }
                System.Windows.Forms.MessageBox.Show("Parsing Complete.", "Script Parser");
            }
            else
            {
                return;
            }
        }
    }
}
