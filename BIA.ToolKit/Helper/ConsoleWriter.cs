namespace BIA.ToolKit.Helper
{
    using BIA.ToolKit.Application.Helper;
    using BIA.ToolKit.Dialogs;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    public class ConsoleWriter : IConsoleWriter
    {
        TextBlock OutputText;
        ScrollViewer OutputTextViewer;
        Window WindowOwner;
        List<Message> messages = new List<Message>();
        List<string> displayedMessages = new List<string>();

        public ConsoleWriter()
        {

        }

        public void InitOutput(TextBlock _outputText, ScrollViewer _outputTextViewer, Window _windowOwner)
        {
            OutputText = _outputText;
            OutputTextViewer = _outputTextViewer;
            WindowOwner = _windowOwner;
        }

        public struct Message
        {
            public string message;
            public string color;
        }

        public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
        {
            if (!refreshimediate)
            {
                messages.Add(new Message { message = message, color = color });
            }
            else
            {
                if (messages.Count >0)
                {
                    Run run = new Run(@"[?? OPEN LOG DETAIL]" + "\r\n");
                    run.Foreground = Brushes.YellowGreen;
                    run.Cursor = Cursors.Hand;
                    run.TextDecorations = TextDecorations.Underline;
                    run.MouseDown += new MouseButtonEventHandler(OpenDetail);
                    run.DataContext = messages;
                    OutputText.Inlines.Add(run);

                    messages = new List<Message>();
                }
                //foreach (Message msg in messages)
                //{
                //    _AddMsgLine(msg.message, msg.color, refreshimediate);
                //}
                //messages.Clear();
                AddMsgLine(OutputText, OutputTextViewer, message, color, refreshimediate);
                displayedMessages.Add(message);
            }
        }

        private void OpenDetail(object sender, MouseButtonEventArgs e)
        {
            var dialog = new LogDetailUC { Owner = WindowOwner };

            // Display the dialog box and read the response
            bool? result = dialog.ShowDialog((List<Message>)((Run)sender).DataContext);
        }

        public void Clear()
        {
            displayedMessages.Clear();
            OutputText.Inlines.Clear();
        }

        public void CopyToClipboard()
        {
            Clipboard.SetText(string.Join(Environment.NewLine, displayedMessages));
        }

        public static void AddMsgLine(TextBlock OutputText, ScrollViewer OutputTextViewer, string message, string color, bool refreshimediate = true)
        {
            Brush brush = null;
            if (string.IsNullOrEmpty(color))
            {
                brush = Brushes.White;
            }
            else
            {
                Color col = (Color)ColorConverter.ConvertFromString(color);
                brush = new SolidColorBrush(col);
            }
            AddMessageLine(OutputText, OutputTextViewer, message, brush, refreshimediate);
        }

        public static void AddMessageLine(TextBlock OutputText, ScrollViewer OutputTextViewer, string message, Brush brush, bool refreshimediate = true)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(
                DispatcherPriority.Normal,
            (ThreadStart)delegate
            {

                Run run = new Run(message + "\r\n");
                run.Foreground = brush;
                OutputText.Inlines.Add(run);
                if (refreshimediate)
                {
                    if (OutputTextViewer.VerticalOffset == OutputTextViewer.ScrollableHeight)
                    {
                        OutputTextViewer.ScrollToEnd();
                    }
                    System.Windows.Forms.Application.DoEvents();
                }
            });
        }
    }
}
