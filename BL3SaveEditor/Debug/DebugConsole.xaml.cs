using System;
using System.ComponentModel;
using System.Windows;
using BL3Tools.TMSUnpack;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;
using MessageBoxImage = AdonisUI.Controls.MessageBoxImage;
using Ookii.Dialogs.Wpf;
using System.IO;

namespace BL3SaveEditor.Debug {
    /// <summary>
    /// Interaction logic for DebugConsole.xaml
    /// </summary>
    public partial class DebugConsole : Window {
        public ConsoleRedirectWriter consoleRedirectWriter;
        public bool bClose = false;

        public DebugConsole() {
            this.Hide();

            InitializeComponent();

            consoleRedirectWriter = new ConsoleRedirectWriter(textBoxDebug);
        }

        protected override void OnClosing(CancelEventArgs e) {
            if(!bClose) {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            try {
                var data = TMSUnpacker.DownloadFromURL();
                // Now we write the folders out
                var vx = new VistaFolderBrowserDialog() {
                    SelectedPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                    ShowNewFolderButton = true
                };

                if(vx.ShowDialog() == true) {
                    foreach(TMSArchive.TMSFile tmsFile in data.Files) {
                        string filePath = Path.Combine(vx.SelectedPath, tmsFile.FileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        File.WriteAllBytes(filePath, tmsFile.Contents);
                    }
                }
            }
            catch(Exception ex) {
                MessageBox.Show(string.Format("Error unpacking TMS: {0}", ex.Message));
            }
            
            return;
        }
    }
}
