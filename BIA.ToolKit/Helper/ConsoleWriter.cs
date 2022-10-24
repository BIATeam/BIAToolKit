namespace BIA.ToolKit.Helper
{
    using BIA.ToolKit.Application.Helper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using System.Windows.Threading;

    public class ConsoleWriter : IConsoleWriter
    {
        TextBlock OutputText;
        ScrollViewer OutputTextViewer;
        Queue queue = new Queue();

        public ConsoleWriter()
        {

        }

        public void InitOutput(TextBlock _outputText, ScrollViewer _outputTextViewer)
        {
            OutputText = _outputText;
            OutputTextViewer = _outputTextViewer;
        }

        struct Message
        {
            public string message;
            public string color;
        }

        public void AddMessageLine(string message, string color = null, bool refreshimediate = true)
        {
            if (!refreshimediate)
            {
                queue.Enqueue(new Message { message = message, color = color });
            }
            else
            {
                foreach (Message msg in queue)
                {
                    _AddMsgLine(msg.message, msg.color, refreshimediate);
                }
                queue.Clear();
                _AddMsgLine(message, color, refreshimediate);
            }
            
        }

        private void _AddMsgLine(string message, string color, bool refreshimediate)
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
            AddMessageLine(message, brush, refreshimediate);
        }

        public void AddMessageLine(string message, Brush brush, bool refreshimediate = true)
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
