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
        List<Message> messages = [];
        readonly List<string> displayedMessages = [];

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
                if (messages.Count > 0)
                {
                    var run = new Run(@"[🔍 OPEN LOG DETAIL]" + "\r\n")
                    {
                        Foreground = Brushes.YellowGreen,
                        Cursor = Cursors.Hand,
                        TextDecorations = TextDecorations.Underline
                    };
                    run.MouseDown += new MouseButtonEventHandler(OpenDetail);
                    run.DataContext = messages;
                    OutputText.Inlines.Add(run);

                    messages = [];
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
            _ = dialog.ShowDialog((List<Message>)((Run)sender).DataContext);
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
            Brush brush;
            if (string.IsNullOrEmpty(color))
            {
                brush = Brushes.White;
            }
            else
            {
                var col = (Color)ColorConverter.ConvertFromString(color);
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

                var run = new Run(message + "\r\n")
                {
                    Foreground = brush
                };
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
