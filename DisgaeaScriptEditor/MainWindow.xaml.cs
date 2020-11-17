using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf;

using formApp = System.Windows.Forms.Application;
using formMBox = System.Windows.Forms.MessageBox;

namespace DisgaeaScriptEditor
{
    public partial class MainWindow : Window 
    {
        public static string UserFile;
        public static string UserFolder;
        public static string Extension;
        public static byte[] WorkingFile;
        public static string WorkingScript;

        public static int maxVal;
        public static double curVal;

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
                formMBox.Show("Unpacking Complete.", "Script Unpacker");
                progressBar.Value = 0;
                ofd.FileName = null;
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
                formMBox.Show("Packing Complete.", "Script Packer");
                fbd.SelectedPath = null;
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
            Extension = Path.GetExtension(UserFile);

            if (!string.IsNullOrEmpty(UserFile))
            {
                if (Extension == ".BIN" || Extension == ".bin")
                {
                    WorkingFile = File.ReadAllBytes(UserFile);
                    Formats.BIN.Parse();
                    formMBox.Show("Parsing Complete.", "Script Parser");
                    ofd.FileName = null;
                }
                else
                {
                    formMBox.Show("The selected file was not a valid Disgaea Script.", "Script Parser");
                    ofd.FileName = null;
                    return;
                }
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
                if (Directory.GetFiles(UserFolder, "*.bin").Length != 0)
                {
                    maxVal = Directory.EnumerateFiles(UserFolder, "*.bin").Count();
                    progressBar.Maximum = maxVal;

                    foreach (string scrfile in Directory.EnumerateFiles(UserFolder, "*.bin"))
                    {
                        curVal = progressBar.Value;
                        progressBar.Value = curVal + 1;
                        formApp.DoEvents();

                        UserFile = scrfile;
                        WorkingFile = File.ReadAllBytes(UserFile);
                        Formats.BIN.Parse();
                    }
                    formMBox.Show("Parsing Complete.", "Script Parser");
                    progressBar.Value = 0;
                    fbd.SelectedPath = null;
                }
                else
                {
                    formMBox.Show("No valid files for parsing were found.", "Script Parser");
                    fbd.SelectedPath = null;
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            ofd.Filter = "Script File(*.txt)|*.txt";
            ofd.ShowDialog();
            UserFile = ofd.FileName;
            Extension = Path.GetExtension(UserFile);

            if (!string.IsNullOrEmpty(UserFile))
            {
                if (Extension == ".TXT" || Extension == ".txt")
                {
                    WorkingScript = System.IO.Path.GetFullPath(UserFile);
                    Formats.BIN.Compile();
                    formMBox.Show("Compiling Complete.", "Script Compiler");
                    ofd.FileName = null;
                }
                else
                {
                    formMBox.Show("The selected file was not a valid Disgaea Script.", "Script Compiler");
                    ofd.FileName = null;
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void CompileAllButton_Click(object sender, RoutedEventArgs e)
        {
            fbd.ShowDialog();
            UserFolder = fbd.SelectedPath;

            if (!string.IsNullOrEmpty(UserFolder))
            {
                if (Directory.GetFiles(UserFolder, "*.txt").Length != 0)
                {
                    maxVal = Directory.EnumerateFiles(UserFolder, "*.txt").Count();
                    progressBar.Maximum = maxVal;

                    foreach (string txtfile in Directory.EnumerateFiles(UserFolder, "*.txt"))
                    {
                        curVal = progressBar.Value;
                        progressBar.Value = curVal + 1;
                        formApp.DoEvents();

                        UserFile = txtfile;
                        WorkingScript = System.IO.Path.GetFullPath(UserFile);
                        Formats.BIN.Compile();
                    }
                    formMBox.Show("Compiling Complete.", "Script Compiler");
                    progressBar.Value = 0;
                    fbd.SelectedPath = null;
                }
                else
                {
                    formMBox.Show("No valid files for parsing were found.", "Script Parser");
                    fbd.SelectedPath = null;
                    return;
                }
            }
            else
            {
                return;
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            formMBox.Show("Programmed By: Krisan Thyme\nSpecial Thanks: XKeeper, FireFly, and xdaniel", "About");
        }
    }
}
