using System;
using System.ComponentModel;
using System.Windows;

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
    }
}
