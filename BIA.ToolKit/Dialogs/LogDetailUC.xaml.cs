namespace BIA.ToolKit.Dialogs
{
    using BIA.ToolKit.Helper;
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

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class LogDetailUC : Window
    {
        List<ConsoleWriter.Message> Messages = new List<ConsoleWriter.Message>();
        public LogDetailUC()
        {
            InitializeComponent();
        }

        internal bool? ShowDialog(List<ConsoleWriter.Message> messages)
        {
            Messages = messages;
            foreach (ConsoleWriter.Message msg in messages)
            {
                ConsoleWriter.AddMsgLine(OutputDetailText, OutputDetailTextViewer, msg.message, msg.color);
            }
            return ShowDialog();
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            string text = string.Empty;
            foreach (ConsoleWriter.Message msg in Messages)
            {
                text += msg.message;
                text += Environment.NewLine;
            }
            System.Windows.Forms.Clipboard.SetText(text);
        }
    }
}
