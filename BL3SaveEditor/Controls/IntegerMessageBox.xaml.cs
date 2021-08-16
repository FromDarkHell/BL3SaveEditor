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

namespace BL3SaveEditor.Controls {
    /// <summary>
    /// Interaction logic for IntegerMessageBox.xaml
    /// </summary>
    public partial class IntegerMessageBox {

        /// <summary>
        /// Minimum value for the integer message box
        /// </summary>
        public int Minimum { get; set; }

        /// <summary>
        /// Maximum value for the integer message box
        /// </summary>
        public int Maximum { get; set; }

        /// <summary>
        /// Whether or not the user agreed to save their inputted value
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// The last inputted value; regardless of whether or not the user saved/okayed the message box
        /// </summary>
        public int Result { get; set; }

        /// <summary>
        /// The message displayed to the users when they open the box
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// The text labelling the input box
        /// </summary>
        public string Label { get; private set; } = "";

        public IntegerMessageBox(string message, string label, int minimum = int.MinValue, int maximum = int.MaxValue, int defaultValue = 0) {
            InitializeComponent();

            this.Message = message;
            this.Label = label;
            this.Minimum = minimum;
            this.Maximum = maximum;
            this.Result = defaultValue;

            this.DataContext = null;
            this.DataContext = this;
        }

        private void OkBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            this.Succeeded = true;
            this.Close();
        }

        private void ExitBtn_Click(object sender, System.Windows.RoutedEventArgs e) {
            this.Succeeded = false;
            this.Close();
        }
    }
}
